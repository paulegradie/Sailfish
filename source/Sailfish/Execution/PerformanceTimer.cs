using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sailfish.Execution;

public sealed class PerformanceTimer
{
    public readonly List<IterationPerformance> ExecutionIterationPerformances = new();
    private readonly Stopwatch _iterationTimer;
    private DateTimeOffset _executionIterationStart;

    private DateTimeOffset _testCaseStart;
    private DateTimeOffset _testCaseStop;

    public PerformanceTimer()
    {
        _iterationTimer = new Stopwatch();
    }

    public bool IsValid { get; private set; } = true;

    // Overhead calibration diagnostics (populated by the iterator/core invoker)
    public int? OverheadBaselineTicks { get; internal set; }
    public double? OverheadDriftPercent { get; internal set; }
    public int? OverheadWarmupCount { get; internal set; }
    public int? OverheadSampleCount { get; internal set; }

    // Number of iterations where overhead subtraction was capped by the 80% guardrail
    public int CappedIterationCount { get; internal set; }
    public bool OverheadEstimationDisabled { get; internal set; }


    public void SetAsInvalid()
    {
        IsValid = false;
    }

    private bool _overheadApplied;

    public void ApplyOverheadEstimate(int overheadEstimate)
    {
        // Apply exactly once. GetPerformanceResults() re-invokes this on every call, and the engine reads
        // results more than once per case (it builds the TestCaseExecutionResult, then calls ToExternal()
        // for the completion notification). Without this guard the per-call overhead was subtracted
        // repeatedly, under-reporting fast benchmarks — latent until the calibrator started returning a
        // real, non-zero overhead.
        if (_overheadApplied) return;

        // Don't consume the one-shot guard before any samples exist. GetPerformanceResults() is also
        // called at the top of the iteration strategies (to read the test-case start time for the time
        // budget). Today that read happens before OverheadEstimate is assigned, so it's a no-op — but
        // were the guard burned against an empty list here, overhead subtraction would be silently
        // skipped for every real sample collected afterwards. Only arm the guard once we've actually
        // applied the estimate to recorded samples.
        if (ExecutionIterationPerformances.Count == 0) return;

        foreach (var executionIterationPerformance in ExecutionIterationPerformances)
        {
            executionIterationPerformance.ApplyOverheadEstimate(overheadEstimate);
        }
        // accumulate how many iterations were capped by guardrail
        CappedIterationCount = ExecutionIterationPerformances.Sum(x => x.CappedCount);

        _overheadApplied = true;
    }

    public void SetTestCaseStart()
    {
        _testCaseStart = DateTimeOffset.UtcNow;
    }

    public void SetTestCaseStop()
    {
        _testCaseStop = DateTimeOffset.UtcNow;
    }

    public void StartSailfishMethodExecutionTimer()
    {
        if (_iterationTimer.IsRunning) return;
        // UtcNow avoids the timezone resolution that DateTimeOffset.Now performs on every iteration;
        // this timestamp is captured outside the measured region and only records wall-clock metadata.
        _executionIterationStart = DateTimeOffset.UtcNow;
        _iterationTimer.Start();
    }

    public void StopSailfishMethodExecutionTimer(int operationsPerInvoke = 1)
    {
        if (!_iterationTimer.IsRunning) return;
        _iterationTimer.Stop();
        var executionIterationStop = DateTimeOffset.UtcNow;
        // Normalize to per-operation time. When a measured iteration batches N invocations
        // (OperationsPerInvoke), divide the aggregate by N so the recorded sample is the cost of a
        // single operation. This keeps reported statistics per-operation and comparable across
        // methods and runs regardless of batch size. Dividing here (before overhead subtraction)
        // is required: the overhead estimate is per-call, so it must be subtracted from a per-op value.
        var ops = operationsPerInvoke < 1 ? 1 : operationsPerInvoke;
        // Divide the batch in floating point so the per-operation duration keeps sub-tick resolution
        // (integer division truncated toward zero, biasing every batched sample slightly low).
        var perOperationTicks = (double)_iterationTimer.ElapsedTicks / ops;
        ExecutionIterationPerformances.Add(new IterationPerformance(_executionIterationStart, executionIterationStop, perOperationTicks));
        _iterationTimer.Reset();
    }

    public DateTimeOffset GetIterationStartTime()
    {
        return _testCaseStart;
    }

    public DateTimeOffset GetIterationStopTime()
    {
        return _testCaseStop;
    }
}