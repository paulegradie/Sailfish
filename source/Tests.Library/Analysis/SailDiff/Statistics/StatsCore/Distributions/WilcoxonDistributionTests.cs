using System;
using System.Linq;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class WilcoxonDistributionTests
{
    [Fact]
    public void Constructor_WithExactMode_SmallN_PicksExactPath()
    {
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: true);

        distribution.ShouldNotBeNull();
        distribution.Exact.ShouldBeTrue();
    }

    [Fact]
    public void Constructor_WithApproximationMode_DropsExactPath()
    {
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: false);

        distribution.ShouldNotBeNull();
        distribution.Exact.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_BeyondExactCap_FallsBackToApproximation()
    {
        // ExactMaxN is 50 — N=60 must fall through to the normal approximation even when
        // the caller asked for exact.
        var ranks = Enumerable.Range(1, 60).Select(i => (double)i).ToArray();
        var distribution = new WilcoxonDistribution(ranks, exact: true);

        distribution.Exact.ShouldBeFalse();
    }

    [Fact]
    public void Constructor_WithTiedRanks_FallsBackToApproximation()
    {
        // The DP assumes untied integer ranks 1..N. Tied input routes through the normal
        // approximation with tie-corrected variance.
        var ranks = new[] { 1.0, 2.5, 2.5, 4.0, 5.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: true);

        distribution.Exact.ShouldBeFalse();
    }

    [Fact]
    public void Correction_DefaultsToMidpoint()
    {
        var distribution = new WilcoxonDistribution([1.0, 2.0, 3.0], exact: false);
        distribution.Correction.ShouldBe(ContinuityCorrection.Midpoint);
    }

    [Fact]
    public void Mean_CalculatesCorrectly()
    {
        // Mean of W+ under the null = N(N+1)/4. For N=4: 4·5/4 = 5.
        var ranks = new[] { 1.0, 2.0, 3.0, 4.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: false);

        distribution.Mean.ShouldBe(5.0);
    }

    [Fact]
    public void Support_InExactMode_StartsAtZero()
    {
        var distribution = new WilcoxonDistribution([1.0, 2.0, 3.0], exact: true);
        distribution.Support.Min.ShouldBe(0.0);
        distribution.Support.Max.ShouldBe(double.PositiveInfinity);
    }

    [Fact]
    public void Support_InApproxMode_IsBilateralInfinite()
    {
        var distribution = new WilcoxonDistribution([1.0, 2.0, 3.0], exact: false);
        distribution.Support.Min.ShouldBe(double.NegativeInfinity);
        distribution.Support.Max.ShouldBe(double.PositiveInfinity);
    }

    [Fact]
    public void WPositive_AllPositive_SumsAllRanks()
    {
        WilcoxonDistribution.WPositive([1, 1, 1], [1.0, 2.0, 3.0]).ShouldBe(6.0);
    }

    [Fact]
    public void WPositive_AllNegative_ReturnsZero()
    {
        WilcoxonDistribution.WPositive([-1, -1, -1], [1.0, 2.0, 3.0]).ShouldBe(0.0);
    }

    [Fact]
    public void WPositive_MixedSigns_SumsPositiveRanksOnly()
    {
        WilcoxonDistribution.WPositive([1, -1, 1, -1], [1.0, 2.0, 3.0, 4.0]).ShouldBe(4.0);
    }

    [Fact]
    public void DistributionFunction_WithApproximation_NearMeanGivesHalf()
    {
        var ranks = Enumerable.Range(1, 15).Select(i => (double)i).ToArray();
        var distribution = new WilcoxonDistribution(ranks, exact: false);
        var result = distribution.DistributionFunction(distribution.Mean);

        result.ShouldBe(0.5, 0.1);
    }

    [Fact]
    public void ProbabilityDensityFunction_InExactMode_ReturnsNonNegativeAtIntegerSupport()
    {
        var ranks = new[] { 1.0, 2.0, 3.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: true);

        distribution.ProbabilityDensityFunction(3.0).ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ToString_IncludesSignature()
    {
        var distribution = new WilcoxonDistribution([1.0, 2.0, 3.0], exact: false);
        distribution.ToString().ShouldContain("W+(x");
    }

    [Fact]
    public void Constructor_WithZeroRanks_FiltersThem()
    {
        // Zero ranks correspond to before == after pairs that contribute nothing to W+.
        // They should be filtered before computing the distribution.
        var ranks = new[] { 0.0, 1.0, 2.0, 0.0, 3.0 };
        var distribution = new WilcoxonDistribution(ranks, exact: true);

        distribution.ShouldNotBeNull();
        // After filtering: effective n = 3, mean = 3*4/4 = 3.
        distribution.Mean.ShouldBe(3.0, 1e-9);
    }

    [Fact]
    public void InverseDistributionFunction_WithApproximation_NearHalfReturnsMean()
    {
        var ranks = Enumerable.Range(1, 15).Select(i => (double)i).ToArray();
        var distribution = new WilcoxonDistribution(ranks, exact: false);
        var result = distribution.InverseDistributionFunction(0.5);

        result.ShouldBe(distribution.Mean, 1.0);
    }
}
