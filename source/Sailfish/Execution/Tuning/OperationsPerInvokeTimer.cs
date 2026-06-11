using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sailfish.Execution.Tuning;

/// <summary>
///     Times a batch of operations for <see cref="OperationsPerInvokeTuner"/>'s pilot measurements.
///     Production uses the wall-clock <see cref="StopwatchOperationsPerInvokeTimer"/>; tests substitute a
///     scripted source so the tuner's <em>decision</em> logic (batch growth → per-op estimate → chosen
///     OPI) can be exercised deterministically. Real pilot timing carries unbounded jitter under CI load,
///     which made any assertion on "a fast method tunes up" inherently flaky — under load a single
///     invocation can appear to already fill the target iteration, leaving OPI pinned at 1.
/// </summary>
internal interface IOperationsPerInvokeTimer
{
    /// <summary>Invokes <paramref name="invocation"/> <paramref name="operations"/> times and returns the aggregate duration in milliseconds.</summary>
    Task<double> TimeBatchAsync(Func<Task> invocation, int operations);
}

/// <summary>Wall-clock implementation used in production runs.</summary>
internal sealed class StopwatchOperationsPerInvokeTimer : IOperationsPerInvokeTimer
{
    public async Task<double> TimeBatchAsync(Func<Task> invocation, int operations)
    {
        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < operations; i++)
        {
            await invocation().ConfigureAwait(false);
        }
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
    }
}
