using System.Linq;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Trawl;

public class LatencyStatsCalculatorTests
{
    [Fact]
    public void Empty_Input_Returns_AllZero()
    {
        var stats = LatencyStatsCalculator.Compute(new double[0]);

        stats.Min.ShouldBe(0);
        stats.Max.ShouldBe(0);
        stats.Mean.ShouldBe(0);
        stats.P99.ShouldBe(0);
    }

    [Fact]
    public void SingleSample_AllPercentilesEqualThatSample()
    {
        var stats = LatencyStatsCalculator.Compute(new[] { 42.0 });

        stats.Min.ShouldBe(42.0);
        stats.Max.ShouldBe(42.0);
        stats.Mean.ShouldBe(42.0);
        stats.P50.ShouldBe(42.0);
        stats.P99.ShouldBe(42.0);
    }

    [Fact]
    public void NearestRankPercentiles_OnOneToHundred()
    {
        // 1..100, shuffled, to prove the calculator sorts internally.
        var data = Enumerable.Range(1, 100).Select(i => (double)i).Reverse().ToArray();

        var stats = LatencyStatsCalculator.Compute(data);

        stats.Min.ShouldBe(1);
        stats.Max.ShouldBe(100);
        stats.Mean.ShouldBe(50.5);
        stats.P50.ShouldBe(50);  // ceil(0.50*100)=50 -> index 49 -> value 50
        stats.P90.ShouldBe(90);
        stats.P95.ShouldBe(95);
        stats.P99.ShouldBe(99);
    }
}
