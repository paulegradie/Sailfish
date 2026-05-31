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

    public void ApplyOverheadEstimate(int overheadEstimate)
    {
        foreach (var executionIterationPerformance in ExecutionIterationPerformances)
        {
            executionIterationPerformance.ApplyOverheadEstimate(overheadEstimate);
        }
        // accumulate how many iterations were capped by guardrail
        CappedIterationCount = ExecutionIterationPerformances.Sum(x => x.CappedCount);
    }

    public void SetTestCaseStart()
    {
        _testCaseStart = DateTimeOffset.Now;
    }

    public void SetTestCaseStop()
    {
        _testCaseStop = DateTimeOffset.Now;
    }

    public void StartSailfishMethodExecutionTimer()
    {
        if (_iterationTimer.IsRunning) return;
        _executionIterationStart = DateTimeOffset.Now;
        _iterationTimer.Start();
    }

    public void StopSailfishMethodExecutionTimer(int operationsPerInvoke = 1)
    {
        if (!_iterationTimer.IsRunning) return;
        _iterationTimer.Stop();
        var executionIterationStop = DateTimeOffset.Now;
        // Normalize to per-operation time. When a measured iteration batches N invocations
        // (OperationsPerInvoke), divide the aggregate by N so the recorded sample is the cost of a
        // single operation. This keeps reported statistics per-operation and comparable across
        // methods and runs regardless of batch size. Dividing here (before overhead subtraction)
        // is required: the overhead estimate is per-call, so it must be subtracted from a per-op value.
        var ops = operationsPerInvoke < 1 ? 1 : operationsPerInvoke;
        var perOperationTicks = _iterationTimer.ElapsedTicks / ops;
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