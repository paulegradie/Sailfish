using System.Diagnostics;

namespace VeerPerforma.Execution;

public class IterationPerformance
{
    public IterationPerformance(DateTimeOffset startTime, DateTimeOffset endTime, long elapsedMilliseconds)
    {
        StartTime = startTime;
        StopTime = StopTime;
        Duration = elapsedMilliseconds;
    }

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset StopTime { get; set; }
    public long Duration { get; set; } // milliseconds
}

public class PerformanceTimer
{
    public PerformanceTimer()
    {
        globalTimer = new Stopwatch();
        methodTimer = new Stopwatch();
        executionTimer = new Stopwatch();
    }

    public DateTimeOffset GlobalStart { get; private set; }
    public DateTimeOffset GlobalStop { get; private set; }
    public TimeSpan GlobalDuration { get; private set; }

    public readonly List<IterationPerformance> MethodIterationPerformances = new(); // all iterations of the method
    public readonly List<IterationPerformance> ExecutionIterationPerformances = new();
    private readonly Stopwatch globalTimer;
    private readonly Stopwatch methodTimer;
    private readonly Stopwatch executionTimer;

    // transient fields
    private DateTimeOffset methodIterationStart;
    private DateTimeOffset executionIterationStart;

    public void StartExecutionTimer()
    {
        if (executionTimer.IsRunning) return;
        executionIterationStart = DateTimeOffset.Now;
        executionTimer.Start();
    }

    public void StopExecutionTimer()
    {
        if (!executionTimer.IsRunning) return;
        executionTimer.Stop();
        var executionIterationStop = DateTimeOffset.Now;
        ExecutionIterationPerformances.Add(new IterationPerformance(executionIterationStart, executionIterationStop, executionTimer.ElapsedMilliseconds));
        executionTimer.Reset();
    }

    public void StartMethodTimer()
    {
        if (methodTimer.IsRunning) return;
        methodIterationStart = DateTimeOffset.Now;
        methodTimer.Start();
    }

    public void StopMethodTimer()
    {
        if (!methodTimer.IsRunning) return;
        methodTimer.Stop();
        var methodIterationStop = DateTimeOffset.Now;
        MethodIterationPerformances.Add(new IterationPerformance(methodIterationStart, methodIterationStop, methodTimer.ElapsedMilliseconds));
        methodTimer.Reset();
    }

    public void StartGlobalTimer()
    {
        if (globalTimer.IsRunning) return;
        GlobalStart = DateTimeOffset.Now;
        globalTimer.Start();
    }

    public void StopGlobalTimer()
    {
        if (!globalTimer.IsRunning) return;
        globalTimer.Stop();
        GlobalStop = DateTimeOffset.Now;
        GlobalDuration = GlobalStop - GlobalStart;
        globalTimer.Reset();
    }
}