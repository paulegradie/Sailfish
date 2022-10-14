using System;

namespace Sailfish.Execution;

public class IterationPerformance
{
    public IterationPerformance(DateTimeOffset startTime, DateTimeOffset endTime, long elapsedMilliseconds)
    {
        StartTime = startTime;
        StopTime = endTime;
        Duration = elapsedMilliseconds;
    }

    public DateTimeOffset StartTime { get; }
    public DateTimeOffset StopTime { get; }
    public long Duration { get; } // milliseconds
}