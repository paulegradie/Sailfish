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

    [Fact]
    public void ApplyOverheadEstimate_OnEmptySamples_DoesNotConsumeGuard()
    {
        // GetPerformanceResults() reads the timer at the top of the iteration strategies (before any
        // sample is recorded) to fetch the test-case start time. If that empty read armed the one-shot
        // guard, every real sample collected afterwards would silently skip overhead subtraction.
        var timer = new PerformanceTimer();

        // Read while empty — must be a no-op that leaves the guard un-armed.
        timer.ApplyOverheadEstimate(100);

        var now = DateTimeOffset.UtcNow;
        timer.ExecutionIterationPerformances.Add(new IterationPerformance(now, now, 1000));
        var before = timer.ExecutionIterationPerformances[0].GetDurationFromTicks().NanoSeconds.Duration;

        // Now that a sample exists, the estimate must actually be subtracted.
        timer.ApplyOverheadEstimate(100);
        var after = timer.ExecutionIterationPerformances[0].GetDurationFromTicks().NanoSeconds.Duration;

        after.ShouldBeLessThan(before);
    }
}
