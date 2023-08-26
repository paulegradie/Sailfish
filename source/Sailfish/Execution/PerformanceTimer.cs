using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sailfish.Execution;

public sealed class PerformanceTimer
{
    public readonly List<IterationPerformance> ExecutionIterationPerformances = new();
    private readonly Stopwatch executionTimer;
    private readonly Stopwatch globalTimer;
    public readonly List<IterationPerformance> MethodIterationPerformances = new(); // all iterations of the method
    private readonly Stopwatch methodIterationTimer;
    private DateTimeOffset executionIterationStart;

    // transient fields
    private DateTimeOffset methodIterationStart;

    public PerformanceTimer()
    {
        globalTimer = new Stopwatch();
        methodIterationTimer = new Stopwatch();
        executionTimer = new Stopwatch();
    }

    public DateTimeOffset GlobalStart { get; private set; }
    public DateTimeOffset GlobalStop { get; private set; }
    public TimeSpan GlobalDuration { get; private set; }
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

    public void StartSailfishMethodExecutionTimer()
    {
        if (executionTimer.IsRunning) return;
        executionIterationStart = DateTimeOffset.Now;
        executionTimer.Start();
    }

    public void StopSailfishMethodExecutionTimer()
    {
        if (!executionTimer.IsRunning) return;
        executionTimer.Stop();
        var executionIterationStop = DateTimeOffset.Now;
        ExecutionIterationPerformances.Add(new IterationPerformance(executionIterationStart, executionIterationStop, executionTimer.ElapsedTicks));
        executionTimer.Reset();
    }

    public void StartMethodLifecycleTimer()
    {
        if (methodIterationTimer.IsRunning) return;
        methodIterationStart = DateTimeOffset.Now;
        methodIterationTimer.Start();
    }

    public void StopMethodLifecycleTimer()
    {
        if (!methodIterationTimer.IsRunning) return;
        methodIterationTimer.Stop();
        var methodIterationStop = DateTimeOffset.Now;
        MethodIterationPerformances.Add(new IterationPerformance(methodIterationStart, methodIterationStop, methodIterationTimer.ElapsedTicks));
        methodIterationTimer.Reset();
    }

    public void StartGlobalLifecycleTimer()
    {
        if (globalTimer.IsRunning) return;
        GlobalStart = DateTimeOffset.Now;
        globalTimer.Start();
    }

    public void StopGlobalLifecycleTimer()
    {
        if (!globalTimer.IsRunning) return;
        globalTimer.Stop();
        GlobalStop = DateTimeOffset.Now;
        GlobalDuration = GlobalStop - GlobalStart;
        globalTimer.Reset();
    }
}