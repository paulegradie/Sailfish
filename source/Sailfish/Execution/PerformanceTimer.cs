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

    public void StopSailfishMethodExecutionTimer()
    {
        if (!_iterationTimer.IsRunning) return;
        _iterationTimer.Stop();
        var executionIterationStop = DateTimeOffset.Now;
        ExecutionIterationPerformances.Add(new IterationPerformance(_executionIterationStart, executionIterationStop, _iterationTimer.ElapsedTicks));
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