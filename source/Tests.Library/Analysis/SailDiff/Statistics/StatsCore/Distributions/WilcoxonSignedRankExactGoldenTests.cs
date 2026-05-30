using System.Linq;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore.Distributions;

/// <summary>
/// Hand-derivable goldens for the exact null distribution of the Wilcoxon signed-rank
/// statistic <c>W+</c>. Values below come from direct enumeration of the 2^N sign
/// combinations for small N — independent of which algorithm produces them. Any
/// implementation that matches these is computing the right distribution.
/// </summary>
public class WilcoxonSignedRankExactGoldenTests
{
    // For N = 3 the ranks are {1, 2, 3}. The 2^3 = 8 sign combinations give W+ values:
    //   (---) → 0,  (--+) → 3,  (-+-) → 2,  (-++) → 5,
    //   (+--) → 1,  (+-+) → 4,  (++-) → 3,  (+++) → 6
    // Counts at w = 0..6: 1, 1, 1, 2, 1, 1, 1.  Total = 8.
    private static readonly double[] Expected_N3_Pmf =
    [
        1.0 / 8, 1.0 / 8, 1.0 / 8, 2.0 / 8, 1.0 / 8, 1.0 / 8, 1.0 / 8
    ];

    // For N = 4 the ranks are {1, 2, 3, 4} and there are 2^4 = 16 sign combinations.
    // Counts of subsets of {1..4} summing to w, w = 0..10:
    //   0 → {} (1),    1 → {1} (1),    2 → {2} (1),    3 → {3},{1,2} (2),
    //   4 → {4},{1,3} (2),    5 → {1,4},{2,3} (2),
    //   6 → {2,4},{1,2,3} (2),    7 → {3,4},{1,2,4} (2),
    //   8 → {1,3,4} (1),   9 → {2,3,4} (1),   10 → {1,2,3,4} (1).
    private static readonly double[] Expected_N4_Pmf =
    [
        1.0 / 16, 1.0 / 16, 1.0 / 16, 2.0 / 16, 2.0 / 16,
        2.0 / 16, 2.0 / 16, 2.0 / 16, 1.0 / 16, 1.0 / 16, 1.0 / 16
    ];

    [Fact]
    public void Pmf_N3_MatchesHandDerivedReference()
    {
        var pmf = WilcoxonSignedRankExactCdf.Pmf(3);
        pmf.Length.ShouldBe(Expected_N3_Pmf.Length);
        for (var w = 0; w < pmf.Length; w++)
            pmf[w].ShouldBe(Expected_N3_Pmf[w], tolerance: 1e-12, customMessage: $"PMF(W+={w})");
    }

    [Fact]
    public void Pmf_N4_MatchesHandDerivedReference()
    {
        var pmf = WilcoxonSignedRankExactCdf.Pmf(4);
        pmf.Length.ShouldBe(Expected_N4_Pmf.Length);
        for (var w = 0; w < pmf.Length; w++)
            pmf[w].ShouldBe(Expected_N4_Pmf[w], tolerance: 1e-12, customMessage: $"PMF(W+={w})");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public void Pmf_SumsToOne(int n)
    {
        // Probabilities must sum to 1 within floating-point tolerance.
        var pmf = WilcoxonSignedRankExactCdf.Pmf(n);
        pmf.Sum().ShouldBe(1.0, tolerance: 1e-9);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(10)]
    public void Pmf_IsSymmetricAboutMean(int n)
    {
        // The null distribution of W+ is symmetric about its mean N(N+1)/4 because each
        // sign assignment has its mirror with the same probability. Equivalently
        // P(W+ = w) == P(W+ = N(N+1)/2 - w).
        var pmf = WilcoxonSignedRankExactCdf.Pmf(n);
        var maxW = n * (n + 1) / 2;
        for (var w = 0; w <= maxW; w++)
            pmf[w].ShouldBe(pmf[maxW - w], tolerance: 1e-12,
                customMessage: $"P(W+={w}) ≠ P(W+={maxW - w}) for N={n}");
    }

    [Fact]
    public void Pmf_N0_HasSingleMassAtZero()
    {
        var pmf = WilcoxonSignedRankExactCdf.Pmf(0);
        pmf.ShouldBe([1.0]);
    }

    [Fact]
    public void Pmf_N1_HasUniformOverTwoOutcomes()
    {
        // N=1: ranks = {1}, sign assignments: (-) → W+=0, (+) → W+=1.
        var pmf = WilcoxonSignedRankExactCdf.Pmf(1);
        pmf.ShouldBe([0.5, 0.5]);
    }

    [Fact]
    public void DistributionFunction_ExactPath_MatchesCumulativePmf()
    {
        // The cumulative sums of the PMF must agree with the CDF the distribution exposes.
        // This is the bridge test between WilcoxonSignedRankExactCdf and WilcoxonDistribution.
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var dist = new WilcoxonDistribution(ranks, exact: true);
        var pmf = WilcoxonSignedRankExactCdf.Pmf(5);
        var cumulative = 0.0;
        for (var w = 0; w < pmf.Length; w++)
        {
            cumulative += pmf[w];
            dist.DistributionFunction(w).ShouldBe(cumulative, tolerance: 1e-12,
                customMessage: $"CDF(W+={w}) for N=5");
        }
    }
}
