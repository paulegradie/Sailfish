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

    // Tracks how many times this iteration's overhead subtraction was capped by the 80% guardrail
    public int CappedCount { get; private set; }

    public TimeResult GetDurationFromTicks()
    {
        return TickAutoConverter.ConvertToTime(ElapsedTicks);
    }

    public void ApplyOverheadEstimate(int overheadEstimate)
    {
        // Cap subtraction to at most 80% of this iteration's ticks to avoid oversubtraction on microbenchmarks
        var maxSubtract = (int)Math.Floor(ElapsedTicks * 0.8);
        if (maxSubtract < 0) maxSubtract = 0;
        if (overheadEstimate > maxSubtract) CappedCount++;
        var subtract = Math.Min(overheadEstimate, maxSubtract);
        if (ElapsedTicks - subtract < 0) return;
        ElapsedTicks -= subtract;
    }
}