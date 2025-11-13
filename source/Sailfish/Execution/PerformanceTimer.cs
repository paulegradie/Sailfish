using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sailfish.Execution;

public sealed class PerformanceTimer
{
    public readonly List<IterationPerformance> ExecutionIterationPerformances = new();
    private readonly Stopwatch iterationTimer;
    private DateTimeOffset executionIterationStart;

    private DateTimeOffset testCaseStart;
    private DateTimeOffset testCaseStop;

    public PerformanceTimer()
    {
        iterationTimer = new Stopwatch();
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
        testCaseStart = DateTimeOffset.Now;
    }

    public void SetTestCaseStop()
    {
        testCaseStop = DateTimeOffset.Now;
    }

    public void StartSailfishMethodExecutionTimer()
    {
        if (iterationTimer.IsRunning) return;
        executionIterationStart = DateTimeOffset.Now;
        iterationTimer.Start();
    }

    public void StopSailfishMethodExecutionTimer()
    {
        if (!iterationTimer.IsRunning) return;
        iterationTimer.Stop();
        var executionIterationStop = DateTimeOffset.Now;
        ExecutionIterationPerformances.Add(new IterationPerformance(executionIterationStart, executionIterationStop, iterationTimer.ElapsedTicks));
        iterationTimer.Reset();
    }

    public DateTimeOffset GetIterationStartTime()
    {
        return testCaseStart;
    }

    public DateTimeOffset GetIterationStopTime()
    {
        return testCaseStop;
    }
}