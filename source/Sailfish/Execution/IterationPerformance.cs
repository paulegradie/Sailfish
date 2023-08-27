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
    private long ElapsedTicks { get; set; }

    public TimeResult GetDurationFromTicks()
    {
        return TickAutoConverter.ConvertToTime(ElapsedTicks);
    }

    public void ApplyOverheadEstimate(int overheadEstimate)
    {
        if (ElapsedTicks - overheadEstimate < 0) return;
        ElapsedTicks -= overheadEstimate;
    }
}