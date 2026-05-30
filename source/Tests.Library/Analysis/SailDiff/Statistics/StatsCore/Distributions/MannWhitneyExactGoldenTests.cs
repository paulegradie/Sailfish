using System;
using System.Linq;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.Factories;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Contracts.Public;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Statistics.StatsCore.Distributions;

/// <summary>
/// Golden / equivalence tests for the Mann-Whitney exact null distribution. These pin the
/// behavior we must preserve while replacing the combinatorial enumeration with a DP
/// recurrence. Every CDF value here is either:
/// <list type="bullet">
/// <item><description>Derivable by hand from first principles (small N like 3,3 and 4,4) and
/// matches the published reference distribution — see Mann & Whitney (1947) and Conover,
/// Practical Nonparametric Statistics, 3rd ed., Table A6.</description></item>
/// <item><description>A reference value from R's <c>pwilcox(q, m, n)</c> with no ties.</description></item>
/// </list>
/// If any of these tests start failing after the implementation swap, the rewrite has
/// changed the math — not just the cost.
/// </summary>
public class MannWhitneyExactGoldenTests
{
    // ─── Reference PMF: P(U = u) for two independent samples of sizes (n1, n2). ───────────
    // Total arrangements = C(n1+n2, min(n1,n2)). The values below were derived by hand and
    // match the standard published tables (see Hollander, Wolfe & Chicken, "Nonparametric
    // Statistical Methods", 3rd ed., Table A.6).
    //
    // n1 = n2 = 3, total = C(6,3) = 20, U ∈ {0..9}
    // PMF * 20 = 1, 1, 2, 3, 3, 3, 3, 2, 1, 1
    // CDF * 20 = 1, 2, 4, 7, 10, 13, 16, 18, 19, 20
    private static readonly double[] Expected_N3_N3_Cdf =
    [
        1.0 / 20, 2.0 / 20, 4.0 / 20, 7.0 / 20, 10.0 / 20,
        13.0 / 20, 16.0 / 20, 18.0 / 20, 19.0 / 20, 20.0 / 20
    ];

    // n1 = n2 = 4, total = C(8,4) = 70, U ∈ {0..16}
    // PMF * 70 = 1, 1, 2, 3, 5, 5, 7, 7, 8, 7, 7, 5, 5, 3, 2, 1, 1
    // (symmetric around U = 8; counts validated by exhaustive enumeration of C(8,4) = 70)
    private static readonly double[] Expected_N4_N4_Cdf =
    [
        1.0 / 70, 2.0 / 70, 4.0 / 70, 7.0 / 70, 12.0 / 70,
        17.0 / 70, 24.0 / 70, 31.0 / 70, 39.0 / 70, 46.0 / 70,
        53.0 / 70, 58.0 / 70, 63.0 / 70, 66.0 / 70, 68.0 / 70,
        69.0 / 70, 70.0 / 70
    ];

    [Fact]
    public void Cdf_N3_N3_MatchesHandDerivedReference()
    {
        // Untied ranks 1..6 for samples of size 3,3.
        var ranks = new double[] { 1, 2, 3, 4, 5, 6 };
        var dist = new MannWhitneyDistribution(ranks, 3, 3);

        for (var u = 0; u < Expected_N3_N3_Cdf.Length; u++)
        {
            var cdf = dist.DistributionFunction(u);
            cdf.ShouldBe(Expected_N3_N3_Cdf[u], tolerance: 1e-9,
                customMessage: $"CDF(U={u}) for N1=N2=3");
        }
    }

    [Fact]
    public void Cdf_N4_N4_MatchesHandDerivedReference()
    {
        var ranks = new double[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var dist = new MannWhitneyDistribution(ranks, 4, 4);

        for (var u = 0; u < Expected_N4_N4_Cdf.Length; u++)
        {
            var cdf = dist.DistributionFunction(u);
            cdf.ShouldBe(Expected_N4_N4_Cdf[u], tolerance: 1e-9,
                customMessage: $"CDF(U={u}) for N1=N2=4");
        }
    }

    [Fact]
    public void ComplementaryCdf_N4_N4_FollowsDiscreteSemantics()
    {
        // Pin a non-obvious but intentional behavioural quirk: in the *exact* (discrete) path,
        // the implementation defines:
        //   DistributionFunction(x)              = P(U ≤ x)
        //   ComplementaryDistributionFunction(x) = P(U ≥ x)
        // so the two sum to 1 + P(U = x), NOT 1. That convention is used by the rank-sum
        // wrapper to compute the two-tailed p-value as 2 · min(P(U ≤ U_obs), P(U ≥ U_obs)) —
        // the correct discrete formula where U_obs is counted in both tails. Any rewrite must
        // preserve this so the wrapper's downstream p-value calculation continues to work.
        var ranks = new double[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var dist = new MannWhitneyDistribution(ranks, 4, 4);

        for (var u = 0; u <= 16; u++)
        {
            var cdf = dist.DistributionFunction(u);
            var ccdf = dist.ComplementaryDistributionFunction(u);
            var pmf = dist.ProbabilityDensityFunction(u);
            (cdf + ccdf).ShouldBe(1.0 + pmf, tolerance: 1e-9,
                customMessage: $"CDF + CCDF at U={u} must equal 1 + P(U=u)");
        }
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    [InlineData(3, 5)]
    [InlineData(5, 5)]
    [InlineData(6, 8)]
    public void Cdf_Untied_IsMonotonicNondecreasingOnSupport(int n1, int n2)
    {
        // Any correct CDF must be monotonic non-decreasing over its support.
        var ranks = Enumerable.Range(1, n1 + n2).Select(i => (double)i).ToArray();
        var dist = new MannWhitneyDistribution(ranks, n1, n2);

        double previous = -1;
        for (var u = 0; u <= n1 * n2; u++)
        {
            var cdf = dist.DistributionFunction(u);
            cdf.ShouldBeGreaterThanOrEqualTo(previous,
                customMessage: $"CDF not monotonic at U={u} for n1={n1}, n2={n2}");
            cdf.ShouldBeInRange(0.0, 1.0 + 1e-9);
            previous = cdf;
        }

        // The full support always covers probability 1.
        dist.DistributionFunction(n1 * n2).ShouldBe(1.0, tolerance: 1e-9);
    }

    // Note: an earlier Pmf_Untied_IsSymmetricAroundMean Theory iterated over (mean ± delta)
    // and was vacuous for odd n1·n2 (mean = half-integer → every evaluation at fractional
    // support → PMF returns 0 either side → 0 == 0 passes trivially). Replaced by
    // Pmf_Untied_IsSymmetricOnIntegerSupport below, which iterates over integer u and uses
    // the algebraic identity P(U = u) == P(U = n1·n2 − u) instead.

    [Fact]
    public void Pdf_N3_N3_MatchesHandDerivedReference()
    {
        // PMF for the N=3,3 distribution, derived by hand: counts of U=0..9 are
        // 1,1,2,3,3,3,3,2,1,1 out of 20 arrangements.
        var ranks = new double[] { 1, 2, 3, 4, 5, 6 };
        var dist = new MannWhitneyDistribution(ranks, 3, 3);

        var expectedCounts = new[] { 1, 1, 2, 3, 3, 3, 3, 2, 1, 1 };
        for (var u = 0; u < expectedCounts.Length; u++)
        {
            var pdf = dist.ProbabilityDensityFunction(u);
            pdf.ShouldBe(expectedCounts[u] / 20.0, tolerance: 1e-9,
                customMessage: $"PDF(U={u}) for N1=N2=3");
        }
    }

    // ─── End-to-end p-value goldens via the public wrapper. ──────────────────────────
    // These exercise the test wrapper as it would be called by SailDiff. The expected
    // p-values are computed from the hand-derived CDFs above using the two-tailed formula
    // p = min(1, 2 * min(P(U ≤ u_obs), P(U ≥ u_obs))).

    [Fact]
    public void WrapperPValue_N3_N3_DisjointSamples_MatchesExactReference()
    {
        // before = {1,2,3}, after = {4,5,6} — rank sums are 6 (sample1) and 15 (sample2)
        // U1 = R1 - n1*(n1+1)/2 = 6 - 6 = 0; U2 = R2 - n2*(n2+1)/2 = 15 - 6 = 9.
        // Min(U) = 0. Two-tailed p = 2 * P(U ≤ 0 or U ≥ 9) — by symmetry the tail sum is
        // 2 * (1/20) = 2/20 = 0.10.
        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(
            new double[] { 1, 2, 3 },
            new double[] { 4, 5, 6 },
            new SailDiffSettings(alpha: 0.05, useOutlierDetection: false));

        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.PValue.ShouldBe(0.10, tolerance: 1e-9);
    }

    [Fact]
    public void WrapperPValue_N4_N4_DisjointSamples_MatchesExactReference()
    {
        // before = {1..4}, after = {5..8} — U = 0, two-tailed p = 2 * (1/70) ≈ 0.02857
        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(
            new double[] { 1, 2, 3, 4 },
            new double[] { 5, 6, 7, 8 },
            new SailDiffSettings(alpha: 0.05, useOutlierDetection: false));

        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.PValue.ShouldBe(2.0 / 70.0, tolerance: 1e-9);
    }

    [Fact]
    public void WrapperPValue_N5_N5_FullyDisjoint_MatchesExactReference()
    {
        // Untied inputs — every value in `before` is strictly less than every value in
        // `after`. Ranks become 1..5 and 6..10. U_min = 0 (the extreme of the support).
        // C(10,5) = 252, so P(U = 0) = 1/252 and the two-tailed p-value is 2/252.
        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(
            new double[] { 1, 2, 3, 4, 5 },
            new double[] { 6, 7, 8, 9, 10 },
            new SailDiffSettings(alpha: 0.05, useOutlierDetection: false));

        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.PValue.ShouldBe(2.0 / 252.0, tolerance: 1e-9);
    }

    [Fact]
    public void WrapperPValue_N6_N6_DisjointWithModerateGap_MatchesExactReference()
    {
        // Fully disjoint, all `after` values strictly greater than all `before` values.
        // C(12,6) = 924, U_min = 0, two-tailed p = 2/924.
        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(
            new double[] { 1, 2, 3, 4, 5, 6 },
            new double[] { 7, 8, 9, 10, 11, 12 },
            new SailDiffSettings(alpha: 0.05, useOutlierDetection: false));

        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.PValue.ShouldBe(2.0 / 924.0, tolerance: 1e-9);
    }

    [Fact]
    public void WrapperPValue_N5_N5_InterleavedSamples_NoSignificantDifference()
    {
        // Untied interleaved inputs — ranks alternate, so the U statistic sits near its
        // expected null value (n1*n2/2 = 12.5). Strict significance shouldn't be detected;
        // the wrapper should label this NoChange under any reasonable alpha.
        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(
            new double[] { 1, 3, 5, 7, 9 },
            new double[] { 2, 4, 6, 8, 10 },
            new SailDiffSettings(alpha: 0.05, useOutlierDetection: false));

        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.PValue.ShouldBeGreaterThan(0.05);
        result.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.NoChange);
    }

    [Fact]
    public void WrapperPValue_N10_N10_AtExactPathBoundary_LargeEffectIsDetected()
    {
        // N=10,10 is the lower edge of the exact path under Tier 1's 2M cap (C(20,10) =
        // 184,756). A large effect (5σ shift) must be flagged Regressed.
        var rng = new Random(7);
        var before = Enumerable.Range(0, 10).Select(_ => rng.NextDouble() * 1.0 + 100).ToArray();
        var after = Enumerable.Range(0, 10).Select(_ => rng.NextDouble() * 1.0 + 110).ToArray();

        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(before, after, new SailDiffSettings(alpha: 0.05, useOutlierDetection: false));

        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.PValue.ShouldBeLessThan(0.001);
        result.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.Regressed);
    }

    // ─── Direct factory-level goldens for the underlying MannWhitneyWilcoxon analyser. ───
    // These bypass the test preprocessor so the assertion is on the raw statistical
    // computation. Statistic = min(U1, U2) per the current convention.

    [Fact]
    public void Analyser_N3_N3_Disjoint_ProducesExpectedUStatistic()
    {
        var t = MannWhitneyWilcoxonFactory.Create(
            new double[] { 1, 2, 3 },
            new double[] { 4, 5, 6 });
        // Sample1 ranks {1,2,3}, RankSum1=6, U1 = 6 - 3*4/2 = 0.
        // Sample2 ranks {4,5,6}, RankSum2=15, U2 = 15 - 3*4/2 = 9.
        // Statistic = min, but the current implementation picks based on sample-size order;
        // we just pin both observable values rather than couple to the tie-break logic.
        t.Statistic1.ShouldBe(0.0, tolerance: 1e-9);
        t.Statistic2.ShouldBe(9.0, tolerance: 1e-9);
        t.PValue.ShouldBe(0.10, tolerance: 1e-9);
    }

    [Fact]
    public void TiedRanks_OddSizedGroupAtIntegerMean_RouteToNormalApproximation()
    {
        // Regression for Codex review on PR #245: detecting ties via "rank has fractional
        // part" misses tie groups of odd size, which produce integer mean ranks. For samples
        // [1, 2] vs [2, 2, 3] the combined rank vector is [1, 3, 3, 3, 5] — three rank-3
        // entries from the 3-way tie — and every element is an integer. The pre-fix code
        // routed this to the DP path, which assumes a permutation of 1..N and silently
        // returned the *untied* null distribution.
        //
        // The rank vector here is what the wrapper would compute internally from those
        // samples; we construct it directly to make the assertion specific to the
        // tie-detection branch rather than coupled to the upstream Rank() helper.
        var ranks = new[] { 1.0, 3.0, 3.0, 3.0, 5.0 };
        var dist = new MannWhitneyDistribution(ranks, 2, 3);

        dist.Exact.ShouldBeFalse("3-way tie at integer ranks must fall through to the normal approximation");
    }

    [Fact]
    public void TiedRanks_FiveWayTieAtIntegerMean_RouteToNormalApproximation()
    {
        // Five tied observations at central positions also average to an integer rank.
        // Combined sorted: positions 1..9 with five tied at positions 2..6 → rank = 4 each.
        // Rank vector: [1, 4, 4, 4, 4, 4, 7, 8, 9] — all integer.
        var ranks = new[] { 1.0, 4.0, 4.0, 4.0, 4.0, 4.0, 7.0, 8.0, 9.0 };
        var dist = new MannWhitneyDistribution(ranks, 4, 5);

        dist.Exact.ShouldBeFalse("5-way tie at integer ranks must fall through to the normal approximation");
    }

    [Fact]
    public void TiedRanks_TwoWayTieAtHalfIntegerMean_RouteToNormalApproximation()
    {
        // Two tied observations average to a half-integer rank — the case the original
        // fractional-rank check did handle. This test stays as a sanity guard so the new
        // tie-aware check doesn't accidentally regress the even-sized case.
        var ranks = new[] { 1.0, 2.5, 2.5, 4.0 };
        var dist = new MannWhitneyDistribution(ranks, 2, 2);

        dist.Exact.ShouldBeFalse("2-way tie at half-integer ranks must fall through to the normal approximation");
    }

    [Fact]
    public void UntiedRanks_StillUseExactDpPath()
    {
        // Symmetric guard: the tie-detection rewrite must not over-trigger and downgrade
        // legitimate untied inputs to the normal approximation.
        var ranks = new double[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var dist = new MannWhitneyDistribution(ranks, 4, 4);

        dist.Exact.ShouldBeTrue("untied rank vector should still use the DP exact path");
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(-0.5)]
    [InlineData(9.0)]                       // upper edge — integer, valid PMF
    [InlineData(9.4)]                       // 0.4 away — fractional, fractional-check returns 0
    [InlineData(9.6)]                       // rounds up to 10; fractional-check returns 0
    [InlineData(9.999_999_999_9)]           // CR #3 regression — rounds to 10 AND within 1e-9 → tries to index _pmf[10]
    [InlineData(10.000_000_000_1)]          // just past _pmf.Length; first guard must reject
    [InlineData(100.0)]                     // far beyond the support
    public void Pdf_OutOfSupportQueries_DoNotThrow(double x)
    {
        // Regression for CodeRabbit review on PR #245: a value within 1e-9 of `_pmf.Length`
        // rounds up to an index one past the array AND clears the fractional-tolerance
        // check, throwing an IndexOutOfRangeException. The PMF should saturate to 0 outside
        // the support no matter how the input drifts.
        var ranks = new double[] { 1, 2, 3, 4, 5, 6 };
        var dist = new MannWhitneyDistribution(ranks, 3, 3); // support is {0..9}; _pmf.Length = 10

        var pmf = dist.ProbabilityDensityFunction(x);

        pmf.ShouldBeGreaterThanOrEqualTo(0.0);
        pmf.ShouldBeLessThanOrEqualTo(1.0);
    }

    [Theory]
    [InlineData(3, 3)]
    [InlineData(4, 4)]
    [InlineData(5, 5)]
    [InlineData(8, 8)]
    [InlineData(3, 5)]
    public void Pmf_Untied_IsSymmetricOnIntegerSupport(int n1, int n2)
    {
        // Replaces an earlier formulation that evaluated PMF at `mean ± delta` — for odd
        // n1*n2 the mean is a half-integer, every evaluation lands at fractional support,
        // and the test passed vacuously (0.0 == 0.0). Now we iterate over integer u in
        // {0..n1*n2} and exploit the algebraic symmetry P(U = u) == P(U = n1*n2 - u),
        // which holds for any (n1, n2) regardless of parity.
        var ranks = Enumerable.Range(1, n1 + n2).Select(i => (double)i).ToArray();
        var dist = new MannWhitneyDistribution(ranks, n1, n2);
        var maxU = n1 * n2;

        for (var u = 0; u <= maxU; u++)
        {
            var lowerPmf = dist.ProbabilityDensityFunction(u);
            var upperPmf = dist.ProbabilityDensityFunction(maxU - u);
            lowerPmf.ShouldBe(upperPmf, tolerance: 1e-12,
                customMessage: $"P(U={u}) ≠ P(U={maxU - u}) for n1={n1}, n2={n2}");
        }
    }
}
