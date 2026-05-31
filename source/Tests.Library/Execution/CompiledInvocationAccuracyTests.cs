using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Execution;
using Sailfish.Logging;
using Shouldly;
using Xunit;

namespace Tests.Library.Execution;

/// <summary>
///     End-to-end accuracy checks for the compiled-delegate invocation path: a method that does a
///     known ~25ms of work must measure as ~25ms for every supported return shape. The lower bound is
///     the meaningful assertion — it catches "not awaited" (would read ~0ms) and "not invoked" bugs;
///     the upper bound is loose to tolerate scheduler/timer jitter on CI VMs.
/// </summary>
public class CompiledInvocationAccuracyTests
{
    private const int WorkMs = 25;
    private const int Samples = 5;

    private static async Task<double> MedianMillisecondsAsync(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName)!;
        var settings = new ExecutionSettings { NumWarmupIterations = 1, SampleSize = Samples };
        var container = TestInstanceContainer.CreateTestInstance(
            instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);

        await container.CoreInvoker.ExecutionMethod(CancellationToken.None, timed: false); // warmup
        for (var i = 0; i < Samples; i++)
            await container.CoreInvoker.ExecutionMethod(CancellationToken.None, timed: true);

        var durations = container.CoreInvoker.GetPerformanceResults().ExecutionIterationPerformances
            .Select(p => p.GetDurationFromTicks().MilliSeconds.Duration)
            .OrderBy(x => x)
            .ToArray();
        return durations[durations.Length / 2];
    }

    private static void ShouldBeNearWork(double medianMs)
    {
        medianMs.ShouldBeGreaterThan(WorkMs * 0.5);   // proves the work was actually invoked + timed
        medianMs.ShouldBeLessThan(WorkMs * 6.0 + 50); // generous ceiling for CI jitter
    }

    [Fact]
    public async Task SyncVoid_MeasuresApproxTrueDuration() =>
        ShouldBeNearWork(await MedianMillisecondsAsync(new Targets(), nameof(Targets.SyncSleep)));

    [Fact]
    public async Task AsyncTask_IsAwaitedAndMeasured() =>
        ShouldBeNearWork(await MedianMillisecondsAsync(new Targets(), nameof(Targets.AsyncDelay)));

    [Fact]
    public async Task AsyncTaskWithToken_IsAwaitedAndMeasured() =>
        ShouldBeNearWork(await MedianMillisecondsAsync(new Targets(), nameof(Targets.AsyncDelayToken)));

    [Fact]
    public async Task AsyncValueTask_IsAwaitedAndMeasured() =>
        ShouldBeNearWork(await MedianMillisecondsAsync(new Targets(), nameof(Targets.AsyncValueTaskDelay)));

    // Regression guard for the incidental correctness fix: a method that returns a Task WITHOUT the
    // async keyword used to fall through the legacy "sync" branch and was not awaited (measured ~0ms).
    // The compiled invoker awaits by return type, so it now measures the real duration.
    [Fact]
    public async Task NonAsyncTaskReturning_IsNowAwaitedAndMeasured() =>
        ShouldBeNearWork(await MedianMillisecondsAsync(new Targets(), nameof(Targets.NonAsyncTaskReturning)));

    [Fact]
    public async Task ThrowingMethod_IsCaughtAsFailure_NotCrash()
    {
        var instance = new Targets();
        var method = typeof(Targets).GetMethod(nameof(Targets.Throws))!;
        var settings = new ExecutionSettings { NumWarmupIterations = 0, SampleSize = 2 };
        var container = TestInstanceContainer.CreateTestInstance(
            instance, method, Array.Empty<string>(), Array.Empty<object>(), false, settings);

        var strategy = new FixedIterationStrategy(Substitute.For<ILogger>());
        var result = await strategy.ExecuteIterations(container, settings, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldContain("boom");
    }

    private sealed class Targets
    {
        public void SyncSleep() => Thread.Sleep(WorkMs);
        public async Task AsyncDelay() => await Task.Delay(WorkMs);
        public async Task AsyncDelayToken(CancellationToken ct) => await Task.Delay(WorkMs, ct);
        public async ValueTask AsyncValueTaskDelay() => await Task.Delay(WorkMs);
        public Task NonAsyncTaskReturning() => Task.Delay(WorkMs); // no async keyword
        public void Throws() => throw new InvalidOperationException("boom");
    }
}
