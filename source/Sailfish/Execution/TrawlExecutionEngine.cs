using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Exceptions;
using Sailfish.Logging;

namespace Sailfish.Execution;

internal interface ITrawlExecutionEngine
{
    Task<TestCaseExecutionResult> RunAsync(TestInstanceContainer container, TrawlAttribute attribute, CancellationToken cancellationToken);
}

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
internal sealed class TrawlExecutionEngine : ITrawlExecutionEngine
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
    private readonly ClosedModelScheduler _scheduler;

    public TrawlExecutionEngine(ILogger logger, IRunSettings runSettings, ClosedModelScheduler? scheduler = null)
    {
        _logger = logger;
        _runSettings = runSettings;
        _scheduler = scheduler ?? new ClosedModelScheduler();
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

        LoadRunData runData;
        try
        {
            _logger.Log(LogLevel.Information,
                "      ---- Trawl: {VirtualUsers} virtual users, warmup {Warmup}s, duration {Duration}s ({Model})",
                virtualUsers, warmup.TotalSeconds, duration.TotalSeconds, attribute.Model);

            if (warmup > TimeSpan.Zero)
                await _scheduler.RunAsync(invoke, virtualUsers, warmup, record: false, cancellationToken).ConfigureAwait(false);

            container.CoreInvoker.SetTestCaseStart();
            runData = await _scheduler.RunAsync(invoke, virtualUsers, duration, record: true, cancellationToken).ConfigureAwait(false);
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
            Latency = stats
        };

        InjectSamples(container, runData, frequency);

        _logger.Log(LogLevel.Information,
            "      ---- Trawl result: {Rps:0.#} req/s | p50 {P50:0.##}ms p95 {P95:0.##}ms p99 {P99:0.##}ms max {Max:0.##}ms | errors {Errors}/{Total} ({Rate:0.##%})",
            trawlResult.RequestsPerSecond, stats.P50, stats.P95, stats.P99, stats.Max, trawlResult.TotalErrors, trawlResult.TotalRequests, trawlResult.ErrorRate);

        return new TestCaseExecutionResult(container);
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

        var stride = count > MaxInjectedSamples ? (int)Math.Ceiling(count / (double)MaxInjectedSamples) : 1;
        for (var i = 0; i < count; i += stride)
        {
            var sample = samples[i];
            var startWall = runData.RunStartWallClock + TimeSpan.FromSeconds((double)(sample.StartTimestamp - runData.RunStartTimestamp) / frequency);
            var stopWall = startWall + TimeSpan.FromSeconds((double)sample.LatencyTicks / frequency);
            timer.ExecutionIterationPerformances.Add(new IterationPerformance(startWall, stopWall, sample.LatencyTicks));
        }
    }
}
