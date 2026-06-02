using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

/// <summary>
///     A single successful request's timing, captured with <see cref="Stopwatch" /> timestamps so the
///     scheduler stays allocation-light per request and the engine can derive both latency and a wall-clock
///     position later.
/// </summary>
internal readonly struct RequestSample
{
    public RequestSample(long startTimestamp, long latencyTicks)
    {
        StartTimestamp = startTimestamp;
        LatencyTicks = latencyTicks;
    }

    /// <summary><see cref="Stopwatch.GetTimestamp" /> captured at request start.</summary>
    public long StartTimestamp { get; }

    /// <summary>Elapsed <see cref="Stopwatch" /> ticks for the request.</summary>
    public long LatencyTicks { get; }
}

/// <summary>The merged outcome of a closed-model load run.</summary>
internal sealed class LoadRunData
{
    public LoadRunData(
        IReadOnlyList<RequestSample> samples,
        long successCount,
        long errorCount,
        TimeSpan elapsed,
        long runStartTimestamp,
        DateTimeOffset runStartWallClock)
    {
        Samples = samples;
        SuccessCount = successCount;
        ErrorCount = errorCount;
        Elapsed = elapsed;
        RunStartTimestamp = runStartTimestamp;
        RunStartWallClock = runStartWallClock;
    }

    /// <summary>Per-successful-request latency samples (empty when the run was not recording).</summary>
    public IReadOnlyList<RequestSample> Samples { get; }

    public long SuccessCount { get; }
    public long ErrorCount { get; }
    public TimeSpan Elapsed { get; }
    public long RunStartTimestamp { get; }
    public DateTimeOffset RunStartWallClock { get; }
}

/// <summary>
///     The closed-model load scheduler: a fixed number of virtual users each loop "invoke → invoke again"
///     for a fixed duration, so throughput is emergent. Workers are async <see cref="Task" />s (not threads)
///     so an IO-bound scenario frees its thread while awaiting; each worker keeps its own latency buffer so
///     there is no shared mutable state on the hot path. Buffers are merged single-threaded after the run.
/// </summary>
internal sealed class ClosedModelScheduler
{
    public async Task<LoadRunData> RunAsync(
        Func<CancellationToken, ValueTask> invoke,
        int virtualUsers,
        TimeSpan duration,
        bool record,
        CancellationToken cancellationToken)
    {
        if (invoke is null) throw new ArgumentNullException(nameof(invoke));
        if (virtualUsers < 1) virtualUsers = 1;

        var runStartWallClock = DateTimeOffset.UtcNow;
        var runStartTimestamp = Stopwatch.GetTimestamp();
        var deadlineTimestamp = runStartTimestamp + (long)(Math.Max(0, duration.TotalSeconds) * Stopwatch.Frequency);

        var workers = new Task<WorkerResult>[virtualUsers];
        for (var i = 0; i < virtualUsers; i++)
        {
            // Task.Run so each virtual user is scheduled independently even if a scenario blocks synchronously.
            workers[i] = Task.Run(() => RunWorkerAsync(invoke, deadlineTimestamp, record, cancellationToken), CancellationToken.None);
        }

        var workerResults = await Task.WhenAll(workers).ConfigureAwait(false);

        var elapsedTimestamp = Stopwatch.GetTimestamp() - runStartTimestamp;
        var elapsed = TimeSpan.FromSeconds((double)elapsedTimestamp / Stopwatch.Frequency);

        long success = 0;
        long errors = 0;
        var totalSamples = 0;
        foreach (var worker in workerResults)
        {
            success += worker.SuccessCount;
            errors += worker.ErrorCount;
            totalSamples += worker.Samples.Count;
        }

        var merged = new List<RequestSample>(record ? totalSamples : 0);
        if (record)
        {
            foreach (var worker in workerResults) merged.AddRange(worker.Samples);
        }

        return new LoadRunData(merged, success, errors, elapsed, runStartTimestamp, runStartWallClock);
    }

    private static async Task<WorkerResult> RunWorkerAsync(
        Func<CancellationToken, ValueTask> invoke,
        long deadlineTimestamp,
        bool record,
        CancellationToken cancellationToken)
    {
        var samples = record ? new List<RequestSample>() : null;
        long success = 0;
        long errors = 0;

        while (!cancellationToken.IsCancellationRequested && Stopwatch.GetTimestamp() < deadlineTimestamp)
        {
            var start = Stopwatch.GetTimestamp();
            try
            {
                await invoke(cancellationToken).ConfigureAwait(false);
                var latency = Stopwatch.GetTimestamp() - start;
                success++;
                samples?.Add(new RequestSample(start, latency));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                // A failed request counts toward the error rate but contributes no latency sample —
                // a time-to-failure is rarely comparable to a successful-response latency.
                errors++;
            }
        }

        return new WorkerResult(samples ?? (IReadOnlyList<RequestSample>)Array.Empty<RequestSample>(), success, errors);
    }

    private readonly struct WorkerResult
    {
        public WorkerResult(IReadOnlyList<RequestSample> samples, long successCount, long errorCount)
        {
            Samples = samples;
            SuccessCount = successCount;
            ErrorCount = errorCount;
        }

        public IReadOnlyList<RequestSample> Samples { get; }
        public long SuccessCount { get; }
        public long ErrorCount { get; }
    }
}
