using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

/// <summary>
///     Measures the per-invocation overhead of the harness itself by timing an idle invoker
///     (<see cref="CompiledInvoker.Empty" />) through the exact same loop the workload runs in:
///     stopwatch around <c>await invoke(ct)</c>. Because the idle invoker has the identical delegate
///     shape to a compiled workload invoker, the resulting baseline is structurally identical to the
///     measured path, so subtracting it cancels dispatch/await/timer overhead almost exactly rather
///     than approximating it (the way BenchmarkDotNet subtracts its generated overhead loop).
/// </summary>
internal class HarnessBaselineCalibrator
{
    private const int WarmupCount = 16;
    private const int SampleCount = 64;

    // Exposed for diagnostics consumers
    internal static int Warmups => WarmupCount;
    internal static int Samples => SampleCount;

    /// <summary>
    ///     Returns the median per-invocation overhead, in Stopwatch ticks, of invoking
    ///     <paramref name="idleInvoker" />. Pass <see cref="CompiledInvoker.Empty" /> to measure the
    ///     baseline that is subtracted from the workload.
    /// </summary>
    public async Task<int> CalibrateTicksAsync(Func<CancellationToken, ValueTask> idleInvoker, CancellationToken cancellationToken)
    {
        if (idleInvoker is null) throw new ArgumentNullException(nameof(idleInvoker));

        // Warmup JIT/infra
        for (var i = 0; i < WarmupCount; i++)
        {
            await idleInvoker(cancellationToken).ConfigureAwait(false);
        }

        // Measure N samples
        var samples = new List<long>(SampleCount);
        for (var i = 0; i < SampleCount; i++)
        {
            var sw = Stopwatch.StartNew();
            await idleInvoker(cancellationToken).ConfigureAwait(false);
            sw.Stop();
            samples.Add(sw.ElapsedTicks);
        }

        var median = Median(samples);

        // Clamp to int range and non-negative
        if (median < 0) median = 0;
        if (median > int.MaxValue) median = int.MaxValue;
        return (int)median;
    }

    private static long Median(IReadOnlyList<long> values)
    {
        if (values.Count == 0) return 0;
        var ordered = values.OrderBy(v => v).ToArray();
        var n = ordered.Length;
        if (n % 2 == 1) return ordered[n / 2];
        var a = ordered[(n / 2) - 1];
        var b = ordered[n / 2];
        // average two middles safely
        return a + ((b - a) / 2);
    }
}
