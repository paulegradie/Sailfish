using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Trawl;

public class ArrivalRateSchedulerTests
{
    private static double ToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    [Fact]
    public async Task DispatchesAtBoundedRate_NotAsFastAsPossible()
    {
        var scheduler = new ArrivalRateScheduler();
        Func<CancellationToken, ValueTask> invoke = async ct => await Task.Delay(1, ct);

        // 200 req/s for ~400ms ≈ 80 requests. A closed model with this many in-flight would do far more —
        // the point of the open model is that arrivals are paced by the rate, not by how fast the SUT is.
        var data = await scheduler.RunAsync(invoke, requestsPerSecond: 200, TimeSpan.FromMilliseconds(400), maxInFlight: 50, record: true, CancellationToken.None);

        data.SuccessCount.ShouldBeGreaterThan(0);
        data.SuccessCount.ShouldBeLessThanOrEqualTo(220); // paced — never dispatches ahead of schedule
        data.Samples.Count.ShouldBe((int)data.SuccessCount);
    }

    [Fact]
    public async Task CoordinatedOmission_FoldsQueueingDelayIntoLatency()
    {
        var scheduler = new ArrivalRateScheduler();
        // Schedule 100 req/s through a server that can only serve ~25 req/s (40ms each, one at a time).
        // A naive measurement would report ~40ms for every served request and silently omit the rest;
        // CO correction measures from the intended send time, so the growing backlog shows up as latency.
        Func<CancellationToken, ValueTask> invoke = async ct => await Task.Delay(40, ct);

        var data = await scheduler.RunAsync(invoke, requestsPerSecond: 100, TimeSpan.FromMilliseconds(600), maxInFlight: 1, record: true, CancellationToken.None);

        data.SuccessCount.ShouldBeGreaterThan(0);

        double maxMs = 0;
        foreach (var sample in data.Samples)
        {
            var ms = ToMs(sample.LatencyTicks);
            if (ms > maxMs) maxMs = ms;
        }

        // With a sustained 4x overload the corrected latency must be far larger than the ~40ms service time.
        maxMs.ShouldBeGreaterThan(120);
    }

    [Fact]
    public async Task HonorsCancellation_ReturnsPromptly()
    {
        var scheduler = new ArrivalRateScheduler();
        Func<CancellationToken, ValueTask> invoke = async ct => await Task.Delay(5, ct);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(150));

        var sw = Stopwatch.StartNew();
        var data = await scheduler.RunAsync(invoke, requestsPerSecond: 200, TimeSpan.FromSeconds(10), maxInFlight: 16, record: true, cts.Token);
        sw.Stop();

        sw.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(3));
        data.ShouldNotBeNull();
    }
}
