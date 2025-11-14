using Sailfish.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution.Tuning;

internal class OperationsPerInvokeTuner
{
    private const int WarmupCount = 3;
    private const int SampleCount = 5;
    private const int MaxRefinements = 2;
    private const int MaxOpsPerInvoke = 1_000_000;

    public async Task<int> TuneAsync(
        TestInstanceContainer container,
        TimeSpan targetIterationDuration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (targetIterationDuration <= TimeSpan.Zero)
        {
            return container.ExecutionSettings.OperationsPerInvoke;
        }

        // JIT and cache warmup (invoke main method without timing/recording)
        for (var i = 0; i < WarmupCount; i++)
        {
            await container.CoreInvoker.ExecutionMethod(cancellationToken, timed: false).ConfigureAwait(false);
        }

        // Measure single-operation time across a few samples, take median
        var perOpSamplesMs = new List<double>(SampleCount);
        for (var i = 0; i < SampleCount; i++)
        {
            var sw = Stopwatch.StartNew();
            await container.CoreInvoker.ExecutionMethod(cancellationToken, timed: false).ConfigureAwait(false);
            sw.Stop();
            perOpSamplesMs.Add(sw.Elapsed.TotalMilliseconds);
        }

        var perOpMs = Median(perOpSamplesMs);
        if (perOpMs <= 0)
        {
            // Extremely fast operation; fall back to a small batch
            return Math.Max(1, container.ExecutionSettings.OperationsPerInvoke);
        }

        // Initial estimate
        var targetMs = targetIterationDuration.TotalMilliseconds;
        var ops = (int)Math.Max(1, Math.Round(targetMs / perOpMs));
        ops = Math.Min(ops, MaxOpsPerInvoke);

        // Quick refinement loop using aggregate measurement
        for (var r = 0; r < MaxRefinements; r++)
        {
            var measured = await MeasureAggregateAsync(container, ops, cancellationToken).ConfigureAwait(false);
            if (measured <= 0) break;

            // If we are within 20% of target, stop
            var ratio = measured / targetMs;
            if (ratio >= 0.8 && ratio <= 1.2) break;

            // Proportional adjustment with clamping
            var adjusted = (int)Math.Round(ops * (targetMs / measured));
            adjusted = Math.Clamp(adjusted, 1, MaxOpsPerInvoke);
            if (adjusted == ops) break;
            ops = adjusted;
        }

        logger.Log(LogLevel.Information,
            "      ---- Auto-tuned OperationsPerInvoke: perOp={PerOpMs:F3}ms, target={TargetMs:F1}ms -> OPI={OPI}",
            perOpMs, targetMs, ops);

        return ops;
    }

    private static async Task<double> MeasureAggregateAsync(TestInstanceContainer container, int operations, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < operations; i++)
        {
            await container.CoreInvoker.ExecutionMethod(ct, timed: false).ConfigureAwait(false);
        }
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    private static double Median(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return 0;
        var ordered = values.OrderBy(v => v).ToArray();
        var n = ordered.Length;
        if (n % 2 == 1) return ordered[n / 2];
        var a = ordered[(n / 2) - 1];
        var b = ordered[n / 2];
        return a + ((b - a) / 2.0);
    }
}

