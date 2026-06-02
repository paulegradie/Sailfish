using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Analysis.SailDiff;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Exceptions;
using Sailfish.Logging;
using Sailfish.Trawl;

namespace Sailfish.Execution;

/// <summary>
///     Runs a <c>[Trawl]</c> method as a concurrent load scenario instead of a sequential microbenchmark.
///     <para>
///         It reuses the existing machinery wherever it can: <see cref="CompiledInvoker" /> builds the
///         direct-call delegate from the shared instance (so all virtual users hit one instance —
///         scenario state must be thread-safe), and the per-case lifecycle hooks run via the container's
///         <see cref="CoreInvoker" />. It replaces only the "invoke N times sequentially" loop with the
///         concurrent <see cref="ClosedModelScheduler" />, and replaces the sequential
///         <see cref="PerformanceTimer" /> recording (which is not thread-safe) with per-worker buffers that
///         are merged and injected into the timer single-threaded afterwards — so a Trawl case still reports
///         its latency distribution through the normal pipeline.
///     </para>
/// </summary>
internal sealed class TrawlExecutionEngine
{
    /// <summary>
    ///     Upper bound on the number of latency samples injected into the shared <see cref="PerformanceTimer" />.
    ///     A long load run can produce millions of samples; injecting all of them would bloat the tracking
    ///     file and downstream summary. The authoritative percentiles in <see cref="TrawlResult" /> are always
    ///     computed from the full sample set — this cap only governs what the existing pipeline renders.
    /// </summary>
    private const int MaxInjectedSamples = 10_000;

    private readonly ILogger _logger;
    private readonly IRunSettings _runSettings;
    private readonly ClosedModelScheduler _closedModelScheduler;
    private readonly ArrivalRateScheduler _arrivalRateScheduler;
    private readonly IStatisticalTestExecutor? _statExecutor;

    public TrawlExecutionEngine(
        ILogger logger,
        IRunSettings runSettings,
        ClosedModelScheduler? closedModelScheduler = null,
        ArrivalRateScheduler? arrivalRateScheduler = null,
        IStatisticalTestExecutor? statExecutor = null)
    {
        _logger = logger;
        _runSettings = runSettings;
        _statExecutor = statExecutor;
        _closedModelScheduler = closedModelScheduler ?? new ClosedModelScheduler();
        _arrivalRateScheduler = arrivalRateScheduler ?? new ArrivalRateScheduler();
    }

    public async Task<TestCaseExecutionResult> RunAsync(TestInstanceContainer container, TrawlAttribute attribute, CancellationToken cancellationToken)
    {
        var settings = _runSettings.TrawlSettings;

        var virtualUsers = Math.Max(1, settings.VirtualUsersOverride ?? attribute.VirtualUsers);
        var durationSeconds = attribute.DurationSeconds;
        if (settings.MaxDurationSecondsOverride is { } cap && cap > 0) durationSeconds = Math.Min(durationSeconds, cap);
        var warmupSeconds = settings.WarmupSecondsOverride ?? attribute.WarmupSeconds;

        var duration = TimeSpan.FromSeconds(Math.Max(0, durationSeconds));
        var warmup = TimeSpan.FromSeconds(Math.Max(0, warmupSeconds));

        // Build the direct-call delegate once; all virtual users invoke this single delegate.
        Func<CancellationToken, ValueTask> invoke;
        try
        {
            invoke = CompiledInvoker.Build(container.Instance, container.ExecutionMethod);
        }
        catch (Exception ex)
        {
            return new TestCaseExecutionResult(container, ex);
        }

        // Overhead estimation is meaningless for load scenarios (latency is dominated by the work under test).
        container.CoreInvoker.SetOverheadDisabled(true);

        try
        {
            await container.CoreInvoker.IterationSetup(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new TestCaseExecutionResult(container, ex);
        }

        // Select the load model. Closed: a fixed number of virtual users loop as fast as the system allows.
        // Open: requests arrive at a target rate regardless of in-flight count, with coordinated-omission
        // correction; there VirtualUsers caps concurrent in-flight requests (the connection-pool size).
        Func<TimeSpan, bool, Task<LoadRunData>> runPhase;
        if (attribute.Model == LoadModel.OpenModel)
        {
            var rate = attribute.TargetRequestsPerSecond;
            if (rate <= 0)
            {
                return new TestCaseExecutionResult(container, new SailfishException(
                    $"Trawl scenario '{container.TestCaseId.DisplayName}' uses LoadModel.OpenModel but TargetRequestsPerSecond is not set (must be > 0)."));
            }

            runPhase = (phaseDuration, phaseRecord) =>
                _arrivalRateScheduler.RunAsync(invoke, rate, phaseDuration, maxInFlight: virtualUsers, phaseRecord, cancellationToken);
        }
        else
        {
            runPhase = (phaseDuration, phaseRecord) =>
                _closedModelScheduler.RunAsync(invoke, virtualUsers, phaseDuration, phaseRecord, cancellationToken);
        }

        LoadRunData runData;
        try
        {
            _logger.Log(LogLevel.Information,
                "      ---- Trawl: {Model} | {VirtualUsers} {Unit} | warmup {Warmup}s | duration {Duration}s{RateSuffix}",
                attribute.Model, virtualUsers,
                attribute.Model == LoadModel.OpenModel ? "max in-flight" : "virtual users",
                warmup.TotalSeconds, duration.TotalSeconds,
                attribute.Model == LoadModel.OpenModel ? $" | {attribute.TargetRequestsPerSecond:0.#} req/s target" : string.Empty);

            if (warmup > TimeSpan.Zero)
                await runPhase(warmup, false).ConfigureAwait(false);

            container.CoreInvoker.SetTestCaseStart();
            runData = await runPhase(duration, true).ConfigureAwait(false);
            container.CoreInvoker.SetTestCaseStop();
        }
        catch (Exception ex)
        {
            return new TestCaseExecutionResult(container, ex);
        }
        finally
        {
            try
            {
                await container.CoreInvoker.IterationTearDown(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Warning, ex, "      ---- Trawl iteration teardown threw");
            }
        }

        var frequency = Stopwatch.Frequency;
        var latenciesMs = new double[runData.Samples.Count];
        for (var i = 0; i < runData.Samples.Count; i++)
            latenciesMs[i] = runData.Samples[i].LatencyTicks * 1000.0 / frequency;

        var totalRequests = runData.SuccessCount + runData.ErrorCount;

        // No successful requests is a failed scenario — surface it as a failing test case rather than an
        // empty (and meaningless) latency distribution.
        if (runData.SuccessCount == 0)
        {
            var message = totalRequests == 0
                ? $"Trawl scenario '{container.TestCaseId.DisplayName}' produced no requests in {duration.TotalSeconds:0.##}s."
                : $"Trawl scenario '{container.TestCaseId.DisplayName}' had no successful requests ({runData.ErrorCount} failed).";
            return new TestCaseExecutionResult(container, new SailfishException(message));
        }

        var stats = LatencyStatsCalculator.Compute(latenciesMs);
        var throughput = runData.Elapsed.TotalSeconds > 0 ? totalRequests / runData.Elapsed.TotalSeconds : 0;
        var errorRate = totalRequests > 0 ? (double)runData.ErrorCount / totalRequests : 0;
        var timeSeries = TrawlTimeSeriesCalculator.Compute(runData.Samples, runData.RunStartTimestamp, frequency, runData.Elapsed.TotalSeconds);

        var trawlResult = new TrawlResult
        {
            DisplayName = container.TestCaseId.DisplayName,
            Model = attribute.Model,
            VirtualUsers = virtualUsers,
            Duration = runData.Elapsed,
            TotalRequests = totalRequests,
            TotalErrors = runData.ErrorCount,
            RequestsPerSecond = throughput,
            ErrorRate = errorRate,
            Latency = stats,
            LatencySamplesMs = Downsample(latenciesMs),
            TimeSeries = timeSeries
        };

        // Compare against the latest prior run BEFORE persisting this one (so it isn't its own baseline).
        var verdict = TryCompareRegression(trawlResult);
        if (verdict is not null)
            _logger.Log(LogLevel.Information, "      ---- Trawl regression: {Verdict}", verdict.Message);

        InjectSamples(container, runData, frequency);
        ReportAndPersist(trawlResult);

        if (verdict is not null && _runSettings.TrawlSettings.FailOnRegression && verdict.Outcome == TrawlRegressionOutcome.Regressed)
            return new TestCaseExecutionResult(container, new SailfishException($"Trawl regression gate failed — {verdict.Message}"));

        return new TestCaseExecutionResult(container);
    }

    private TrawlRegressionVerdict? TryCompareRegression(TrawlResult current)
    {
        if (_statExecutor is null || current.LatencySamplesMs.Length == 0) return null;

        var baseline = new TrawlBaselineProvider().GetLatestBaseline(current.DisplayName, _runSettings.LocalOutputDirectory);
        if (baseline is null || baseline.Result.LatencySamplesMs.Length == 0) return null;

        return new TrawlRegressionAnalyzer(_statExecutor)
            .Compare(baseline.Result.LatencySamplesMs, current.LatencySamplesMs, BuildTrawlSailDiffSettings());
    }

    private SailDiffSettings BuildTrawlSailDiffSettings()
    {
        var source = _runSettings.SailDiffSettings;
        // Keep the slow tail (no outlier removal) — for a load test the tail is the signal, not noise.
        return new SailDiffSettings(alpha: source.Alpha, round: 3, useOutlierDetection: false, testType: source.TestType);
    }

    private void ReportAndPersist(TrawlResult result)
    {
        var report = TrawlReportRenderer.Render(result, _runSettings.DistributionPlotStyle);
        _logger.Log(LogLevel.Information, "{TrawlReport}", Environment.NewLine + report);

        // Best-effort: an IO failure persisting artifacts must not fail the test case.
        try
        {
            var writer = new TrawlResultWriter();
            var timestamp = DateTime.UtcNow;
            var outputDirectory = _runSettings.LocalOutputDirectory;
            writer.PersistRecord(result, timestamp, outputDirectory);
            writer.WriteReport(report, result, timestamp, outputDirectory);
            writer.PruneOldRecords(result, outputDirectory, _runSettings.TrawlSettings.MaxRetainedRunsPerScenario);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Warning, ex, "      ---- Trawl: failed to persist result/report");
        }
    }

    /// <summary>
    ///     Uniform down-sampling stride so that at most <see cref="MaxInjectedSamples" /> items survive.
    ///     Shared by <see cref="Downsample" /> and <see cref="InjectSamples" /> so the persisted/plotted sample
    ///     set and the timer-injected set stay in lockstep.
    /// </summary>
    private static int DownsampleStride(int count)
        => count > MaxInjectedSamples ? (int)Math.Ceiling(count / (double)MaxInjectedSamples) : 1;

    /// <summary>Caps the latency sample array for plotting/persistence via uniform stride down-sampling.</summary>
    private static double[] Downsample(double[] all)
    {
        var count = all.Length;
        if (count <= MaxInjectedSamples) return all;

        var stride = DownsampleStride(count);
        var size = (count + stride - 1) / stride;
        var result = new double[size];
        var j = 0;
        for (var i = 0; i < count && j < size; i += stride) result[j++] = all[i];
        return result;
    }

    /// <summary>
    ///     Injects the (optionally down-sampled) latency distribution into the shared
    ///     <see cref="PerformanceTimer" /> single-threaded, so the Trawl case reports through the existing
    ///     console/markdown/tracking pipeline like any other case.
    /// </summary>
    private static void InjectSamples(TestInstanceContainer container, LoadRunData runData, long frequency)
    {
        var timer = container.CoreInvoker.GetPerformanceResults();
        var samples = runData.Samples;
        var count = samples.Count;
        if (count == 0) return;

        var stride = DownsampleStride(count);
        for (var i = 0; i < count; i += stride)
        {
            var sample = samples[i];
            var startWall = runData.RunStartWallClock + TimeSpan.FromSeconds((double)(sample.StartTimestamp - runData.RunStartTimestamp) / frequency);
            var stopWall = startWall + TimeSpan.FromSeconds((double)sample.LatencyTicks / frequency);
            timer.ExecutionIterationPerformances.Add(new IterationPerformance(startWall, stopWall, sample.LatencyTicks));
        }
    }
}
