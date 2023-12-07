using System;

namespace Sailfish.Execution;

public class IterationPerformance(DateTimeOffset startTime, DateTimeOffset endTime, long elapsedTicks)
{
    public DateTimeOffset StartTime { get; } = startTime;
    public DateTimeOffset StopTime { get; } = endTime;
    private long ElapsedTicks { get; set; } = elapsedTicks;

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