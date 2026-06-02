using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

/// <summary>
///     The open-model load scheduler: requests are dispatched at a fixed target arrival rate regardless of
///     how many are already in flight, which is what exposes a system that can't keep up. Concurrency is
///     bounded by <c>maxInFlight</c> (think connection-pool size); when that cap is reached the dispatcher
///     blocks, and because scheduled send times keep advancing at the target rate, the backlog shows up as a
///     growing gap between a request's intended send time and its completion.
///     <para>
///         <b>Coordinated-omission correction.</b> Latency is measured from each request's <i>intended</i>
///         (scheduled) send time, not from when it actually got dispatched. So if the system stalls, the
///         requests that "should" have been sent during the stall are still counted — with the full waiting
///         time folded into their latency — instead of being silently omitted. This is the correction Gil
///         Tene popularized; without it an overloaded system can look deceptively healthy.
///     </para>
/// </summary>
internal sealed class ArrivalRateScheduler
{
    /// <summary>
    ///     How long the post-run drain waits for in-flight requests to finish before abandoning them. A
    ///     scenario that hangs and ignores cancellation must not hang the whole run forever; this bounds the
    ///     worst case to (run duration + this grace).
    /// </summary>
    private static readonly TimeSpan DefaultDrainGrace = TimeSpan.FromSeconds(30);

    public async Task<LoadRunData> RunAsync(
        Func<CancellationToken, ValueTask> invoke,
        double requestsPerSecond,
        TimeSpan duration,
        int maxInFlight,
        bool record,
        CancellationToken cancellationToken,
        TimeSpan? drainTimeout = null)
    {
        if (invoke is null) throw new ArgumentNullException(nameof(invoke));
        if (requestsPerSecond <= 0) requestsPerSecond = 1;
        if (maxInFlight < 1) maxInFlight = 1;

        var runStartWallClock = DateTimeOffset.UtcNow;
        var runStartTimestamp = Stopwatch.GetTimestamp();
        var frequency = (double)Stopwatch.Frequency;
        var deadlineTimestamp = runStartTimestamp + (long)(Math.Max(0, duration.TotalSeconds) * frequency);
        var intervalTicks = frequency / requestsPerSecond;

        var samples = record ? new ConcurrentQueue<RequestSample>() : null;
        long success = 0;
        long errors = 0;
        var inFlight = new SemaphoreSlim(maxInFlight, maxInFlight);

        long index = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            // Stop dispatching once the wall-clock run duration has elapsed. Under overload the index-based
            // schedule falls far behind real time, so this real-time check — not the schedule — bounds the run.
            if (Stopwatch.GetTimestamp() >= deadlineTimestamp) break;

            var scheduledTimestamp = runStartTimestamp + (long)(index * intervalTicks);
            if (scheduledTimestamp >= deadlineTimestamp) break;

            await PaceUntilAsync(scheduledTimestamp, frequency, cancellationToken).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested) break;

            // Acquire an in-flight slot, but bound the wait by the time left until the deadline: under
            // sustained overload every slot can be occupied, and an unbounded wait here would block dispatch
            // (and the whole run) past the run duration — indefinitely if a request hangs. If no slot frees
            // before the deadline, the run is over, so stop dispatching.
            var remainingToDeadline = TimeSpan.FromSeconds((deadlineTimestamp - Stopwatch.GetTimestamp()) / frequency);
            if (remainingToDeadline <= TimeSpan.Zero) break;
            bool acquiredSlot;
            try
            {
                acquiredSlot = await inFlight.WaitAsync(remainingToDeadline, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (!acquiredSlot) break; // deadline reached while saturated

            var scheduled = scheduledTimestamp;
            _ = Task.Run(async () =>
            {
                try
                {
                    await invoke(cancellationToken).ConfigureAwait(false);
                    // Coordinated-omission correction: measure from the intended send time, not the actual one.
                    var latency = Stopwatch.GetTimestamp() - scheduled;
                    Interlocked.Increment(ref success);
                    samples?.Enqueue(new RequestSample(scheduled, latency));
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // shutting down
                }
                catch
                {
                    Interlocked.Increment(ref errors);
                }
                finally
                {
                    inFlight.Release();
                }
            }, CancellationToken.None);

            index++;
        }

        // Drain (bounded): acquiring every permit can only succeed once all in-flight requests have released
        // theirs. A SUT that hangs and ignores cancellation must not hang the whole run forever, so we wait at
        // most the drain grace and then abandon any stragglers. We dispose the semaphore only after a *full*
        // drain — if we abandoned stragglers, one may still Release() it later, so disposing would risk an
        // ObjectDisposedException on that abandoned task.
        var grace = drainTimeout ?? DefaultDrainGrace;
        var graceDeadline = Stopwatch.GetTimestamp() + (long)(Math.Max(0, grace.TotalSeconds) * frequency);
        var acquired = 0;
        while (acquired < maxInFlight)
        {
            var remaining = TimeSpan.FromSeconds((graceDeadline - Stopwatch.GetTimestamp()) / frequency);
            if (remaining <= TimeSpan.Zero) break;
            if (!await inFlight.WaitAsync(remaining, CancellationToken.None).ConfigureAwait(false)) break;
            acquired++;
        }

        if (acquired == maxInFlight) inFlight.Dispose();

        var elapsedTimestamp = Stopwatch.GetTimestamp() - runStartTimestamp;
        var elapsed = TimeSpan.FromSeconds((double)elapsedTimestamp / frequency);

        IReadOnlyList<RequestSample> merged = samples is null
            ? Array.Empty<RequestSample>()
            : new List<RequestSample>(samples);

        return new LoadRunData(merged, success, errors, elapsed, runStartTimestamp, runStartWallClock);
    }

    /// <summary>
    ///     Waits until the scheduled timestamp. Uses <see cref="Task.Delay(TimeSpan, CancellationToken)" /> for
    ///     longer waits and a short spin for sub-millisecond precision. If already on or behind schedule it
    ///     returns immediately — the lateness is intentionally folded into the next request's measured latency.
    /// </summary>
    private static async Task PaceUntilAsync(long targetTimestamp, double frequency, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var remainingTicks = targetTimestamp - Stopwatch.GetTimestamp();
            if (remainingTicks <= 0) return;

            var remainingMs = remainingTicks * 1000.0 / frequency;
            if (remainingMs > 4)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(remainingMs - 2), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
            else
            {
                Thread.SpinWait(50);
            }
        }
    }
}
