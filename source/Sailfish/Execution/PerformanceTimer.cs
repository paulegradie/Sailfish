using System;
using System.Collections.Generic;
using System.Diagnostics;

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