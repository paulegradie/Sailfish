using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

/// <summary>
///     Measures the per-invocation overhead of the harness itself by timing an idle invoker
///     (<see cref="CompiledInvoker.Empty" />) through the exact same loop the workload runs in.
///     Because the idle invoker has the identical delegate shape to a compiled workload invoker, the
///     resulting baseline is structurally identical to the measured path, so subtracting it cancels
///     dispatch/await/timer overhead almost exactly rather than approximating it (the way
///     BenchmarkDotNet subtracts its generated overhead loop).
///     <para>
///         Each timed sample runs a <b>batch</b> of idle invocations and divides by the batch size.
///         Timing a single idle call (the previous approach) quantizes to 0/1 tick on the common coarse
///         timers — Windows QPC (~100 ns/tick) and Apple Silicon (~41.7 ns effective) — so the median
///         collapsed to ~0 and the reported "overhead" was timer quantization noise, not a measurement.
///         Batching recovers per-call overhead well below a single tick, the same technique
///         BenchmarkDotNet uses for its overhead loop.
///     </para>
/// </summary>
internal class HarnessBaselineCalibrator
{
    private const int WarmupCount = 16;
    private const int SampleCount = 64;

    // Idle invocations per timed sample. Large enough that the aggregate window sits well above the
    // timer floor (e.g. 1024 * ~1.5 ns ≈ 1.5 µs, ~15 ticks on a 100 ns clock) so dividing back out
    // yields sub-tick per-call resolution.
    private const int BatchSize = 1024;

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

        // Warmup JIT/infra with full batches
        for (var i = 0; i < WarmupCount; i++)
        {
            for (var j = 0; j < BatchSize; j++)
            {
                await idleInvoker(cancellationToken).ConfigureAwait(false);
            }
        }

        // Measure N batches; each yields a per-call overhead in (fractional) ticks
        var samples = new List<double>(SampleCount);
        for (var i = 0; i < SampleCount; i++)
        {
            var sw = Stopwatch.StartNew();
            for (var j = 0; j < BatchSize; j++)
            {
                await idleInvoker(cancellationToken).ConfigureAwait(false);
            }
            sw.Stop();
            samples.Add((double)sw.ElapsedTicks / BatchSize);
        }

        var median = Median(samples);

        // Round to whole ticks and clamp non-negative. Genuinely sub-tick overhead now rounds to 0
        // (it is below what any single sample can resolve, so subtracting nothing is correct) instead
        // of producing the random 0/1 result single-shot timing used to give.
        var rounded = (long)Math.Round(median, MidpointRounding.AwayFromZero);
        if (rounded < 0) rounded = 0;
        if (rounded > int.MaxValue) rounded = int.MaxValue;
        return (int)rounded;
    }

    private static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return 0;
        var ordered = values.OrderBy(v => v).ToArray();
        var n = ordered.Length;
        if (n % 2 == 1) return ordered[n / 2];
        return 0.5 * (ordered[(n / 2) - 1] + ordered[n / 2]);
    }
}
