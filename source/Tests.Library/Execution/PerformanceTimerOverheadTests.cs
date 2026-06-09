using System;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

public class PerformanceTimerOverheadTests
{
    [Fact]
    public void ApplyOverheadEstimate_IsIdempotent_AcrossRepeatedCalls()
    {
        // CoreInvoker.GetPerformanceResults() re-invokes ApplyOverheadEstimate on every call, and the
        // engine reads the results more than once per case (build result, then ToExternal()). The
        // overhead must be subtracted exactly once regardless of how many times it is applied.
        var timer = new PerformanceTimer();
        var now = DateTimeOffset.UtcNow;
        timer.ExecutionIterationPerformances.Add(new IterationPerformance(now, now, 1000));

        timer.ApplyOverheadEstimate(100);
        var afterFirst = timer.ExecutionIterationPerformances[0].GetDurationFromTicks().NanoSeconds.Duration;

        timer.ApplyOverheadEstimate(100);
        timer.ApplyOverheadEstimate(100);
        var afterRepeat = timer.ExecutionIterationPerformances[0].GetDurationFromTicks().NanoSeconds.Duration;

        afterRepeat.ShouldBe(afterFirst);
    }
}
