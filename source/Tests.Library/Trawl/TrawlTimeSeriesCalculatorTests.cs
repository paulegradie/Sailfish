using System.Collections.Generic;
using System.Diagnostics;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Trawl;

public class TrawlTimeSeriesCalculatorTests
{
    private static readonly long Freq = Stopwatch.Frequency;

    private static long At(double secondsFromStart, long runStart) => runStart + (long)(secondsFromStart * Freq);
    private static long Ms(double ms) => (long)(ms / 1000.0 * Freq);

    [Fact]
    public void Empty_ReturnsEmpty()
    {
        TrawlTimeSeriesCalculator.Compute(new List<RequestSample>(), 0, Freq).RequestsPerSecond.Length.ShouldBe(0);
    }

    [Fact]
    public void BucketsBySecond_CountsThroughput_AndTailLatency()
    {
        const long runStart = 1_000_000;
        var samples = new List<RequestSample>
        {
            // second 0: three requests (10/20/30 ms)
            new(At(0.1, runStart), Ms(10)),
            new(At(0.4, runStart), Ms(20)),
            new(At(0.9, runStart), Ms(30)),
            // second 1: two requests (40/50 ms)
            new(At(1.2, runStart), Ms(40)),
            new(At(1.7, runStart), Ms(50)),
        };

        var ts = TrawlTimeSeriesCalculator.Compute(samples, runStart, Freq);

        ts.SecondOffsets.ShouldBe(new double[] { 0, 1 });
        ts.RequestsPerSecond.ShouldBe(new double[] { 3, 2 }); // one-second buckets => count is req/s
        ts.P99Ms[0].ShouldBe(30, 0.5); // nearest-rank p99 of {10,20,30}
        ts.P99Ms[1].ShouldBe(50, 0.5);
    }
}
