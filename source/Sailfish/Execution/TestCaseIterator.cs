using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Models;
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
    private readonly ILogger logger;
    private readonly IRunSettings runSettings;
    private readonly IIterationStrategy fixedIterationStrategy;
    private readonly IIterationStrategy adaptiveIterationStrategy;

    public TestCaseIterator(
        IRunSettings runSettings,
        ILogger logger,
        IIterationStrategy fixedIterationStrategy,
        IIterationStrategy adaptiveIterationStrategy)
    {
        this.logger = logger;
        this.runSettings = runSettings;
        this.fixedIterationStrategy = fixedIterationStrategy;
        this.adaptiveIterationStrategy = adaptiveIterationStrategy;
    }

    public async Task<TestCaseExecutionResult> Iterate(
        TestInstanceContainer testInstanceContainer,
        bool disableOverheadEstimation,
        CancellationToken cancellationToken)
    {
        var calibrator = new HarnessBaselineCalibrator();
        var warmupResult = await WarmupIterations(testInstanceContainer, cancellationToken);
        if (!warmupResult.IsSuccess) return warmupResult;

        var beginOverheadTicks = 0;
        if (!disableOverheadEstimation)
        {
            beginOverheadTicks = await calibrator.CalibrateTicksAsync(testInstanceContainer.ExecutionMethod, cancellationToken);
        }

        // Determine which strategy to use
        var executionSettings = testInstanceContainer.ExecutionSettings;
        var useAdaptive = executionSettings.UseAdaptiveSampling;

        var strategy = useAdaptive ? adaptiveIterationStrategy : fixedIterationStrategy;

        // Apply sample size override if specified
        if (runSettings.SampleSizeOverride.HasValue)
        {
            if (useAdaptive)
            {
                executionSettings.MaximumSampleSize = Math.Max(runSettings.SampleSizeOverride.Value,
                                                              executionSettings.MinimumSampleSize);
            }
            else
            {
                executionSettings.SampleSize = Math.Max(runSettings.SampleSizeOverride.Value, 1);
            }
        }

            // Auto-tune OperationsPerInvoke to reach target iteration duration, if configured
            if (executionSettings.TargetIterationDuration > TimeSpan.Zero && executionSettings.OperationsPerInvoke <= 1)
            {
                try
                {
                    var tuner = new OperationsPerInvokeTuner();
                    var tuned = await tuner.TuneAsync(testInstanceContainer, executionSettings.TargetIterationDuration, logger, cancellationToken).ConfigureAwait(false);
                    if (tuned > executionSettings.OperationsPerInvoke)
                    {
                        logger.Log(LogLevel.Information, "      ---- Using OperationsPerInvoke={OPI} (auto-tuned)", tuned);
                        executionSettings.OperationsPerInvoke = tuned;
                    }
                }
                catch (Exception ex)
                {
                    // Non-fatal: fall back to provided OperationsPerInvoke
                    logger.Log(LogLevel.Warning, ex, "      ---- Auto-tuning OperationsPerInvoke failed; continuing with OPI={OPI}", executionSettings.OperationsPerInvoke);
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
            logger.Log(LogLevel.Information,
                "      ---- Adaptive sampling completed: {Reason}",
                iterationResult.ConvergenceReason ?? "unknown");
        }

        if (disableOverheadEstimation)
        {
            testInstanceContainer.CoreInvoker.SetOverheadDisabled(true);
            return new TestCaseExecutionResult(testInstanceContainer);
        }

        var endOverheadTicks = await calibrator.CalibrateTicksAsync(testInstanceContainer.ExecutionMethod, cancellationToken);
        var driftPct = beginOverheadTicks > 0
            ? (100.0 * Math.Abs(endOverheadTicks - beginOverheadTicks) / beginOverheadTicks)
            : 0.0;
        if (driftPct > 20.0)
        {
            logger.Log(LogLevel.Warning, "      ---- Overhead drift detected: {DriftPercent}%", driftPct);
        }
        else
        {
            logger.Log(LogLevel.Information, "      ---- Overhead baseline: {OverheadTicks} ticks (median), drift {DriftPercent}%", beginOverheadTicks, driftPct);
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
        for (var i = 0; i < testInstanceContainer.NumWarmupIterations; i++)
        {
            logger.Log(LogLevel.Information, "      ---- warmup iteration {CurrentIteration} of {TotalIterations}", i + 1, testInstanceContainer.NumWarmupIterations);

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


    private TestCaseExecutionResult CatchAndReturn(TestInstanceContainer testProvider, Exception ex)
    {
        if (ex is NullReferenceException)
            ex = new NullReferenceException(ex.Message + Environment.NewLine + $"Null variable or property encountered in method: {testProvider.ExecutionMethod.Name}");

        logger.Log(LogLevel.Error, ex, "An exception occured during test execution");
        return new TestCaseExecutionResult(testProvider, ex);
    }
}