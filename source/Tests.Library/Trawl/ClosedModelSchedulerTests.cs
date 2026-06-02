using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Trawl;

public class ClosedModelSchedulerTests
{
    [Fact]
    public async Task RecordsSamples_AndRunsForRoughlyTheDuration()
    {
        var scheduler = new ClosedModelScheduler();
        Func<CancellationToken, ValueTask> invoke = async ct => await Task.Delay(2, ct);

        var data = await scheduler.RunAsync(invoke, virtualUsers: 4, TimeSpan.FromMilliseconds(300), record: true, CancellationToken.None);

        data.SuccessCount.ShouldBeGreaterThan(0);
        data.ErrorCount.ShouldBe(0);
        data.Samples.Count.ShouldBe((int)data.SuccessCount);
        data.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(250));
    }

    [Fact]
    public async Task WhenNotRecording_NoSamplesButStillCountsSuccesses()
    {
        var scheduler = new ClosedModelScheduler();
        Func<CancellationToken, ValueTask> invoke = async ct => await Task.Delay(2, ct);

        var data = await scheduler.RunAsync(invoke, virtualUsers: 2, TimeSpan.FromMilliseconds(150), record: false, CancellationToken.None);

        data.Samples.Count.ShouldBe(0);
        data.SuccessCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task CountsErrors_WithoutFaultingTheRun()
    {
        var scheduler = new ClosedModelScheduler();
        var counter = 0;
        Func<CancellationToken, ValueTask> invoke = async ct =>
        {
            var n = Interlocked.Increment(ref counter);
            await Task.Delay(1, ct);
            if (n % 2 == 0) throw new InvalidOperationException("boom");
        };

        var data = await scheduler.RunAsync(invoke, virtualUsers: 2, TimeSpan.FromMilliseconds(250), record: true, CancellationToken.None);

        data.SuccessCount.ShouldBeGreaterThan(0);
        data.ErrorCount.ShouldBeGreaterThan(0);
        data.Samples.Count.ShouldBe((int)data.SuccessCount); // failures contribute no latency sample
    }

    [Fact]
    public async Task NeverExceedsVirtualUserConcurrency()
    {
        var scheduler = new ClosedModelScheduler();
        var current = 0;
        var max = 0;
        var gate = new object();
        Func<CancellationToken, ValueTask> invoke = async ct =>
        {
            var c = Interlocked.Increment(ref current);
            lock (gate)
                if (c > max) max = c;
            await Task.Delay(20, ct);
            Interlocked.Decrement(ref current);
        };

        await scheduler.RunAsync(invoke, virtualUsers: 5, TimeSpan.FromMilliseconds(400), record: true, CancellationToken.None);

        max.ShouldBeLessThanOrEqualTo(5); // never more than the configured virtual users
        max.ShouldBeGreaterThanOrEqualTo(2); // genuinely concurrent
    }

    [Fact]
    public async Task HonorsCancellation_ReturnsPromptly()
    {
        var scheduler = new ClosedModelScheduler();
        Func<CancellationToken, ValueTask> invoke = async ct => await Task.Delay(5, ct);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(150));

        var sw = Stopwatch.StartNew();
        // A 10s nominal duration that must be cut short by cancellation.
        var data = await scheduler.RunAsync(invoke, virtualUsers: 4, TimeSpan.FromSeconds(10), record: true, cts.Token);
        sw.Stop();

        sw.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(3));
        data.ShouldNotBeNull();
    }
}
