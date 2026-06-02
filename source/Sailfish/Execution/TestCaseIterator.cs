using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution.Tuning;
using Sailfish.Logging;

namespace Sailfish.Execution;

internal interface ITestCaseIterator
{
    Task<TestCaseExecutionResult> Iterate(
        TestInstanceContainer testInstanceContainer,
        bool disableOverheadEstimation,
        CancellationToken cancellationToken);
}

internal class TestCaseIterator : ITestCaseIterator
{
    private readonly ILogger _logger;
    private readonly IRunSettings _runSettings;
    private readonly IIterationStrategy _fixedIterationStrategy;
    private readonly IIterationStrategy _adaptiveIterationStrategy;

    public TestCaseIterator(
        IRunSettings runSettings,
        ILogger logger,
        IIterationStrategy fixedIterationStrategy,
        IIterationStrategy adaptiveIterationStrategy)
    {
        _logger = logger;
        _runSettings = runSettings;
        _fixedIterationStrategy = fixedIterationStrategy;
        _adaptiveIterationStrategy = adaptiveIterationStrategy;
    }

    public async Task<TestCaseExecutionResult> Iterate(
        TestInstanceContainer testInstanceContainer,
        bool disableOverheadEstimation,
        CancellationToken cancellationToken)
    {
        // Load scenarios ([Trawl]) run concurrently for a duration rather than as sequential timed
        // iterations, so they bypass warmup, overhead calibration, and the iteration strategy entirely.
        var trawlAttribute = testInstanceContainer.ExecutionMethod.GetCustomAttribute<TrawlAttribute>();
        if (trawlAttribute is not null)
        {
            var trawlEngine = new TrawlExecutionEngine(_logger, _runSettings);
            return await trawlEngine.RunAsync(testInstanceContainer, trawlAttribute, cancellationToken).ConfigureAwait(false);
        }

        var calibrator = new HarnessBaselineCalibrator();
        var warmupResult = await WarmupIterations(testInstanceContainer, cancellationToken);
        if (!warmupResult.IsSuccess) return warmupResult;

        var beginOverheadTicks = 0;
        if (!disableOverheadEstimation)
        {
            beginOverheadTicks = await calibrator.CalibrateTicksAsync(CompiledInvoker.Empty, cancellationToken);
        }

        // Determine which strategy to use
        var executionSettings = testInstanceContainer.ExecutionSettings;
        var useAdaptive = executionSettings.UseAdaptiveSampling;

        var strategy = useAdaptive ? _adaptiveIterationStrategy : _fixedIterationStrategy;

        // Apply sample size override if specified
        if (_runSettings.SampleSizeOverride.HasValue)
        {
            if (useAdaptive)
            {
                executionSettings.MaximumSampleSize = Math.Max(_runSettings.SampleSizeOverride.Value,
                                                              executionSettings.MinimumSampleSize);
            }
            else
            {
                executionSettings.SampleSize = Math.Max(_runSettings.SampleSizeOverride.Value, 1);
            }
        }

            // Auto-tune OperationsPerInvoke to reach target iteration duration, if configured
            if (executionSettings.TargetIterationDuration > TimeSpan.Zero && executionSettings.OperationsPerInvoke <= 1)
            {
                try
                {
                    var tuner = new OperationsPerInvokeTuner();
                    var tuned = await tuner.TuneAsync(testInstanceContainer, executionSettings.TargetIterationDuration, _logger, cancellationToken).ConfigureAwait(false);
                    if (tuned > executionSettings.OperationsPerInvoke)
                    {
                        _logger.Log(LogLevel.Information, "      ---- Using OperationsPerInvoke={OPI} (auto-tuned)", tuned);
                        executionSettings.OperationsPerInvoke = tuned;
                    }
                }
                catch (Exception ex)
                {
                    // Non-fatal: fall back to provided OperationsPerInvoke
                    _logger.Log(LogLevel.Warning, ex, "      ---- Auto-tuning OperationsPerInvoke failed; continuing with OPI={OPI}", executionSettings.OperationsPerInvoke);
                }
            }


        testInstanceContainer.CoreInvoker.SetTestCaseStart();

        var iterationResult = await strategy.ExecuteIterations(
            testInstanceContainer,
            executionSettings,
            cancellationToken);

        testInstanceContainer.CoreInvoker.SetTestCaseStop();

        if (!iterationResult.IsSuccess)
        {
            return CatchAndReturn(testInstanceContainer,
                new Exception(iterationResult.ErrorMessage ?? "Iteration failed"));
        }

        // Log convergence information for adaptive sampling
        if (useAdaptive && iterationResult.ConvergedEarly)
        {
            _logger.Log(LogLevel.Information,
                "      ---- Adaptive sampling completed: {Reason}",
                iterationResult.ConvergenceReason ?? "unknown");
        }

        if (disableOverheadEstimation)
        {
            testInstanceContainer.CoreInvoker.SetOverheadDisabled(true);
            return new TestCaseExecutionResult(testInstanceContainer);
        }

        var endOverheadTicks = await calibrator.CalibrateTicksAsync(CompiledInvoker.Empty, cancellationToken);
        // The compiled-delegate path measures near-zero harness overhead, so the calibrator's
        // per-sample value is dominated by Stopwatch start/stop quantization and lands at a small,
        // unstable tick count. A drift *percentage* off such a tiny baseline is just noise (0->1 tick
        // reads as 100%). Gate the drift signal on an absolute-time floor (frequency-independent):
        // below ~half a microsecond, harness overhead can't affect any difference worth resolving.
        const double driftFloorNanoseconds = 500.0;
        var nsPerTick = 1_000_000_000.0 / System.Diagnostics.Stopwatch.Frequency;
        var overheadNanoseconds = beginOverheadTicks * nsPerTick;
        var driftPct = overheadNanoseconds >= driftFloorNanoseconds
            ? (100.0 * Math.Abs(endOverheadTicks - beginOverheadTicks) / beginOverheadTicks)
            : 0.0;
        if (driftPct > 20.0)
        {
            _logger.Log(LogLevel.Warning, "      ---- Overhead drift detected: {DriftPercent}%", driftPct);
        }
        else
        {
            _logger.Log(LogLevel.Information, "      ---- Overhead baseline: {OverheadTicks} ticks (median), drift {DriftPercent}%", beginOverheadTicks, driftPct);
        }

        // Persist diagnostics for test output window consumption
        testInstanceContainer.CoreInvoker.SetOverheadDiagnostics(beginOverheadTicks, driftPct, HarnessBaselineCalibrator.Warmups, HarnessBaselineCalibrator.Samples);

        testInstanceContainer.ApplyOverheadEstimates(beginOverheadTicks);

        return new TestCaseExecutionResult(testInstanceContainer);
    }

    private async Task<TestCaseExecutionResult> WarmupIterations(
        TestInstanceContainer testInstanceContainer,
        CancellationToken cancellationToken)
    {
        return testInstanceContainer.ExecutionSettings.UseSteadyStateWarmup
            ? await SteadyStateWarmupIterations(testInstanceContainer, cancellationToken).ConfigureAwait(false)
            : await FixedWarmupIterations(testInstanceContainer, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TestCaseExecutionResult> FixedWarmupIterations(
        TestInstanceContainer testInstanceContainer,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < testInstanceContainer.NumWarmupIterations; i++)
        {
            _logger.Log(LogLevel.Information, "      ---- warmup iteration {CurrentIteration} of {TotalIterations}", i + 1, testInstanceContainer.NumWarmupIterations);

            try
            {
                await testInstanceContainer.CoreInvoker.IterationSetup(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }

            await testInstanceContainer.CoreInvoker.ExecutionMethod(cancellationToken, false).ConfigureAwait(false);

            try
            {
                await testInstanceContainer.CoreInvoker.IterationTearDown(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }
        }

        return new TestCaseExecutionResult(testInstanceContainer);
    }

    // Warm up until per-iteration timing stops trending and stabilizes (or the cap is hit). Each warmup
    // is timed locally (and NOT recorded into the real samples) and fed to the steady-state detector.
    private async Task<TestCaseExecutionResult> SteadyStateWarmupIterations(
        TestInstanceContainer testInstanceContainer,
        CancellationToken cancellationToken)
    {
        var settings = testInstanceContainer.ExecutionSettings;
        var floor = Math.Max(0, testInstanceContainer.NumWarmupIterations);
        var max = Math.Max(Math.Max(floor, SteadyStateWarmupDetector.DefaultWindow), settings.MaxWarmupIterations);
        var detector = new SteadyStateWarmupDetector();
        var durations = new List<double>(max);

        for (var i = 0; i < max; i++)
        {
            try
            {
                await testInstanceContainer.CoreInvoker.IterationSetup(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }

            var sw = Stopwatch.StartNew();
            await testInstanceContainer.CoreInvoker.ExecutionMethod(cancellationToken, false).ConfigureAwait(false);
            sw.Stop();
            durations.Add(sw.Elapsed.TotalMilliseconds);

            try
            {
                await testInstanceContainer.CoreInvoker.IterationTearDown(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return CatchAndReturn(testInstanceContainer, ex);
            }

            var count = i + 1;
            if (count >= floor && count >= SteadyStateWarmupDetector.DefaultWindow)
            {
                var result = detector.Check(durations, SteadyStateWarmupDetector.DefaultWindow, SteadyStateWarmupDetector.DefaultMaxRelativeDrift, SteadyStateWarmupDetector.DefaultMaxCoefficientOfVariation);
                if (result.ReachedSteadyState)
                {
                    _logger.Log(LogLevel.Information, "      ---- warmup reached steady state after {Count} iterations ({Reason})", count, result.Reason);
                    return new TestCaseExecutionResult(testInstanceContainer);
                }
            }

            _logger.Log(LogLevel.Information, "      ---- warmup iteration {Count} (steady-state; floor {Floor}, max {Max})", count, floor, max);
        }

        _logger.Log(LogLevel.Information, "      ---- warmup reached max {Max} iterations without detecting steady state", max);
        return new TestCaseExecutionResult(testInstanceContainer);
    }


    private TestCaseExecutionResult CatchAndReturn(TestInstanceContainer testProvider, Exception ex)
    {
        if (ex is NullReferenceException)
            ex = new NullReferenceException(ex.Message + Environment.NewLine + $"Null variable or property encountered in method: {testProvider.ExecutionMethod.Name}");

        _logger.Log(LogLevel.Error, ex, "An exception occured during test execution");
        return new TestCaseExecutionResult(testProvider, ex);
    }
}