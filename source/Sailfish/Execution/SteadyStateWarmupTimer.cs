using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sailfish.Execution;

/// <summary>
///     Times a single steady-state warmup invocation. Production uses the wall-clock
///     <see cref="StopwatchWarmupTimer"/>; tests substitute a scripted source so the warmup
///     <em>loop logic</em> (floor / window / early-stop / cap) can be exercised deterministically —
///     real wall-clock durations under CI load carry unbounded jitter, which made any assertion on
///     "a stable method stops early" inherently flaky.
/// </summary>
internal interface ISteadyStateWarmupTimer
{
    /// <summary>Invokes <paramref name="invocation"/> once and returns its duration in milliseconds.</summary>
    Task<double> TimeAsync(Func<Task> invocation);
}

/// <summary>Wall-clock implementation used in production runs.</summary>
internal sealed class StopwatchWarmupTimer : ISteadyStateWarmupTimer
{
    public async Task<double> TimeAsync(Func<Task> invocation)
    {
        var stopwatch = Stopwatch.StartNew();
        await invocation().ConfigureAwait(false);
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }
}
