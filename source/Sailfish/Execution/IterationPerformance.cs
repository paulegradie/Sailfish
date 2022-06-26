using System;

namespace Sailfish.Execution;

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