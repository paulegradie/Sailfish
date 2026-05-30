using System;
using System.Linq;
using Shouldly;
using Xunit;
using EffectSizes = Sailfish.Analysis.SailDiff.Statistics.EffectSizes;

namespace Tests.Library.Analysis.SailDiff;

/// <summary>
/// Hand-derivable goldens for the effect-size and shift-estimator computations.
/// </summary>
public class EffectSizesTests
{
    // ─── Hedges' g ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void HedgesG_EqualSamples_ProducesZero()
    {
        // mean1 == mean2 ⇒ Cohen's d = 0 ⇒ Hedges' g = 0 regardless of variance.
        var result = EffectSizes.HedgesG(
            mean1: 10, var1: 4, n1: 20,
            mean2: 10, var2: 4, n2: 20,
            alpha: 0.05);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe(0.0, tolerance: 1e-9);
        result.Name.ShouldBe("Hedges' g");
    }

    [Fact]
    public void HedgesG_KnownDelta_MatchesHandCalculation()
    {
        // mean1 = 100, mean2 = 102, var1 = var2 = 4 ⇒ s_pool = sqrt(4) = 2.
        // d = (102 − 100) / 2 = 1.0.  J = 1 − 3/(4·(20+20) − 1) = 1 − 3/159 ≈ 0.9811.
        // g = 0.9811 · 1.0 ≈ 0.9811.
        var result = EffectSizes.HedgesG(
            mean1: 100, var1: 4, n1: 20,
            mean2: 102, var2: 4, n2: 20,
            alpha: 0.05);

        result.ShouldNotBeNull();
        result!.Value.ShouldBe(0.9811, tolerance: 1e-3);
    }

    [Fact]
    public void HedgesG_ReportsCi_BracketingPointEstimate()
    {
        // The CI must bracket the point estimate. Specific width depends on alpha + N but
        // basic sanity: lower < value < upper, and width shrinks at smaller alpha.
        var tight = EffectSizes.HedgesG(100, 4, 20, 102, 4, 20, alpha: 0.01);
        var loose = EffectSizes.HedgesG(100, 4, 20, 102, 4, 20, alpha: 0.10);

        tight.ShouldNotBeNull();
        loose.ShouldNotBeNull();
        tight!.CiLower.ShouldNotBeNull();
        tight.CiUpper.ShouldNotBeNull();
        tight.CiLower!.Value.ShouldBeLessThan(tight.Value);
        tight.CiUpper!.Value.ShouldBeGreaterThan(tight.Value);

        // Tighter alpha means wider CI for confidence (1 - alpha = 99% vs 90%).
        var tightWidth = tight.CiUpper.Value - tight.CiLower.Value;
        var looseWidth = loose!.CiUpper!.Value - loose.CiLower!.Value;
        tightWidth.ShouldBeGreaterThan(looseWidth);
    }

    [Fact]
    public void HedgesG_NullForTinySamples()
    {
        EffectSizes.HedgesG(1, 1, 1, 2, 1, 5, 0.05).ShouldBeNull();
        EffectSizes.HedgesG(1, 1, 5, 2, 1, 1, 0.05).ShouldBeNull();
    }

    [Fact]
    public void HedgesG_NullForDegenerateVariance()
    {
        // Both variances zero ⇒ s_pool = 0 ⇒ undefined.
        EffectSizes.HedgesG(1, 0, 5, 2, 0, 5, 0.05).ShouldBeNull();
    }

    // ─── Cliff's delta ─────────────────────────────────────────────────────────────────

    [Fact]
    public void CliffsDelta_AllPairsAfterGreaterThanBefore_ReturnsOne()
    {
        // Every sample2 value strictly greater than every sample1 value ⇒ δ = 1.
        var s1 = new double[] { 1, 2, 3, 4, 5 };
        var s2 = new double[] { 6, 7, 8, 9, 10 };
        var result = EffectSizes.CliffsDelta(s1, s2, alpha: 0.05);

        result.Value.ShouldBe(1.0, tolerance: 1e-12);
        result.Name.ShouldBe("Cliff's delta");
    }

    [Fact]
    public void CliffsDelta_AllPairsAfterLessThanBefore_ReturnsMinusOne()
    {
        var s1 = new double[] { 6, 7, 8, 9, 10 };
        var s2 = new double[] { 1, 2, 3, 4, 5 };
        var result = EffectSizes.CliffsDelta(s1, s2, alpha: 0.05);

        result.Value.ShouldBe(-1.0, tolerance: 1e-12);
    }

    [Fact]
    public void CliffsDelta_IdenticalSamples_ReturnsZero()
    {
        // All pairs are equal ⇒ neither greater nor less ⇒ δ = 0.
        var sample = new double[] { 1, 2, 3, 4, 5 };
        var result = EffectSizes.CliffsDelta(sample, sample, alpha: 0.05);

        result.Value.ShouldBe(0.0, tolerance: 1e-12);
    }

    [Fact]
    public void CliffsDelta_KnownAsymmetry_MatchesHandCount()
    {
        // sample1 = {1, 2, 3}, sample2 = {2, 3, 4}. Pairs (b > a, b < a):
        //   (1,2) (1,3) (1,4) (2,3) (2,4) (3,4) → 6 with b > a
        //   (2,1) (3,1) (3,2) (4,1) (4,2) (4,3) → 6 with b > a  (above)
        //   (2,2) (3,3) → 2 with b = a
        // Wait — I'm listing all 9 (a,b) pairs.
        //   a=1: b=2,3,4 → all greater → 3
        //   a=2: b=2,3,4 → 2 greater (3,4), 1 equal → 2
        //   a=3: b=2,3,4 → 1 less, 1 equal, 1 greater → 1 greater, 1 less
        // So greater = 3 + 2 + 1 = 6, less = 0 + 0 + 1 = 1, total = 9.
        // δ = (6 − 1) / 9 = 5/9 ≈ 0.5556.
        var s1 = new double[] { 1, 2, 3 };
        var s2 = new double[] { 2, 3, 4 };
        var result = EffectSizes.CliffsDelta(s1, s2, alpha: 0.05);

        result.Value.ShouldBe(5.0 / 9.0, tolerance: 1e-12);
    }

    [Fact]
    public void CliffsDelta_CiBoundedInPlusMinusOne()
    {
        // Even when δ is extreme, the reported CI must be clipped to the valid Cliff's
        // delta range. For a near-maximal effect with small N the raw normal-approx CI
        // would exceed 1; the report should clamp.
        var s1 = Enumerable.Range(0, 5).Select(i => (double)i).ToArray();
        var s2 = Enumerable.Range(100, 5).Select(i => (double)i).ToArray();
        var result = EffectSizes.CliffsDelta(s1, s2, alpha: 0.05);

        result.Value.ShouldBe(1.0, tolerance: 1e-12);
        if (result.CiLower.HasValue) result.CiLower.Value.ShouldBeGreaterThanOrEqualTo(-1.0);
        if (result.CiUpper.HasValue) result.CiUpper.Value.ShouldBeLessThanOrEqualTo(1.0);
    }

    // ─── Hodges-Lehmann shift ──────────────────────────────────────────────────────────

    [Fact]
    public void HodgesLehmann_IdenticalSamples_ShiftIsZero()
    {
        var sample = new double[] { 1, 2, 3, 4, 5 };
        var result = EffectSizes.HodgesLehmann(sample, sample, alpha: 0.05);

        result.Value.ShouldBe(0.0, tolerance: 1e-12);
        result.Name.ShouldBe("Hodges-Lehmann shift");
    }

    [Fact]
    public void HodgesLehmann_ConstantShift_RecoversShift()
    {
        // sample2 = sample1 + 5 for every element. All pairwise differences (b − a) are 5.
        // Median = 5.
        var s1 = new double[] { 1, 2, 3, 4, 5 };
        var s2 = new double[] { 6, 7, 8, 9, 10 };
        var result = EffectSizes.HodgesLehmann(s1, s2, alpha: 0.05);

        result.Value.ShouldBe(5.0, tolerance: 1e-12);
    }

    [Fact]
    public void HodgesLehmann_NegativeShift_PreservesSign()
    {
        // sample2 = sample1 − 5 ⇒ median pairwise diff = −5.
        var s1 = new double[] { 6, 7, 8, 9, 10 };
        var s2 = new double[] { 1, 2, 3, 4, 5 };
        var result = EffectSizes.HodgesLehmann(s1, s2, alpha: 0.05);

        result.Value.ShouldBe(-5.0, tolerance: 1e-12);
    }

    [Fact]
    public void HodgesLehmann_KnownAsymmetricShift_MatchesHandMedian()
    {
        // sample1 = {1, 2, 3}, sample2 = {3, 5, 7}. All 9 pairwise (b − a):
        //   3−1=2, 5−1=4, 7−1=6,
        //   3−2=1, 5−2=3, 7−2=5,
        //   3−3=0, 5−3=2, 7−3=4.
        // Sorted: 0, 1, 2, 2, 3, 4, 4, 5, 6. Median (5th of 9) = 3.
        var s1 = new double[] { 1, 2, 3 };
        var s2 = new double[] { 3, 5, 7 };
        var result = EffectSizes.HodgesLehmann(s1, s2, alpha: 0.05);

        result.Value.ShouldBe(3.0, tolerance: 1e-12);
    }

    [Fact]
    public void HodgesLehmann_CiBracketsTheShift()
    {
        // CI must include the point estimate (in a regular case).
        var rng = new Random(7);
        var s1 = Enumerable.Range(0, 30).Select(_ => rng.NextDouble() * 10).ToArray();
        var s2 = Enumerable.Range(0, 30).Select(_ => 5 + rng.NextDouble() * 10).ToArray();
        var result = EffectSizes.HodgesLehmann(s1, s2, alpha: 0.05);

        result.CiLower.ShouldNotBeNull();
        result.CiUpper.ShouldNotBeNull();
        result.CiLower!.Value.ShouldBeLessThanOrEqualTo(result.Value);
        result.CiUpper!.Value.ShouldBeGreaterThanOrEqualTo(result.Value);
    }

    [Fact]
    public void HodgesLehmann_EmptySample_GracefullyReturnsZeroNoCi()
    {
        var result = EffectSizes.HodgesLehmann([], [1.0, 2.0], alpha: 0.05);

        result.Value.ShouldBe(0.0);
        result.CiLower.ShouldBeNull();
        result.CiUpper.ShouldBeNull();
    }

    // ─── Mean difference (Welch) ──────────────────────────────────────────────────────

    [Fact]
    public void MeanDifference_FlipsSignFromAnalyserConvention()
    {
        // TwoSampleT builds its CI on (mean1 − mean2). SailDiff displays After − Before
        // (i.e. mean2 − mean1). Confirm the sign flip is applied.
        var result = EffectSizes.MeanDifference(meanBefore: 10, meanAfter: 15, ciLower: -7, ciUpper: -3);

        result.Value.ShouldBe(5.0, tolerance: 1e-12);
        result.CiLower!.Value.ShouldBe(3.0, tolerance: 1e-12);
        result.CiUpper!.Value.ShouldBe(7.0, tolerance: 1e-12);
        result.Units.ShouldBe("ms");
    }
}
