using Sailfish.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution.Tuning;

internal class OperationsPerInvokeTuner
{
    private const int WarmupCount = 3;
    private const int MaxRefinements = 2;
    private const int MaxOpsPerInvoke = 1_000_000;

    // The aggregate estimate window must be large enough to time reliably above the Stopwatch floor.
    private const double MinMeasurableMs = 2.0;

    private readonly IOperationsPerInvokeTimer _timer;

    public OperationsPerInvokeTuner(IOperationsPerInvokeTimer? timer = null)
    {
        // Wall-clock by default — identical behaviour to the previous inline Stopwatch timing. Tests
        // inject a scripted source so the tuning decision is deterministic under load.
        _timer = timer ?? new StopwatchOperationsPerInvokeTimer();
    }

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

        // Estimate per-operation time from a BATCH, not a single call. Timing one fast invocation
        // quantizes to ~0 on coarse timers, which previously made the tuner give up (return OPI=1) for
        // exactly the sub-microsecond operations that most need batching. Grow the batch geometrically
        // until the aggregate window is large enough to time reliably, then divide back out.
        var batch = 1;
        var batchMs = 0.0;
        while (true)
        {
            batchMs = await MeasureAggregateAsync(container, batch, cancellationToken).ConfigureAwait(false);
            if (batchMs >= MinMeasurableMs || batch >= MaxOpsPerInvoke) break;
            var next = batch * 4;
            if (next <= batch) break; // overflow guard
            batch = Math.Min(next, MaxOpsPerInvoke);
        }

        var perOpMs = batchMs / batch;
        if (perOpMs <= 0)
        {
            // Even the largest batch was unmeasurable; fall back to the configured value.
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
            "      ---- Auto-tuned OperationsPerInvoke: perOp={PerOpMs:F6}ms, target={TargetMs:F1}ms -> OPI={OPI}",
            perOpMs, targetMs, ops);

        return ops;
    }

    private Task<double> MeasureAggregateAsync(TestInstanceContainer container, int operations, CancellationToken ct)
    {
        return _timer.TimeBatchAsync(() => container.CoreInvoker.ExecutionMethod(ct, timed: false), operations);
    }
}
