using System;

namespace Sailfish.Execution;

public class IterationPerformance
{
    public IterationPerformance(DateTimeOffset startTime, DateTimeOffset endTime, long elapsedTicks)
    {
        StartTime = startTime;
        StopTime = endTime;
        ElapsedTicks = elapsedTicks;
    }

    public DateTimeOffset StartTime { get; }
    public DateTimeOffset StopTime { get; }
    private long ElapsedTicks { get; }

    public TimeResult GetDurationFromTicks()
    {
        return TickAutoConverter.ConvertToTime(ElapsedTicks);
    }
}