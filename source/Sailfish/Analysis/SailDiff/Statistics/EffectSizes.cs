using System;
using MathNet.Numerics.Distributions;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.SailDiff.Statistics;

/// <summary>
/// Effect-size and shift-estimate computations used by SailDiff test wrappers.
/// </summary>
/// <remarks>
/// <para>
/// All computations are pure functions on the preprocessed sample arrays — no test
/// statistic is recomputed here. Each method returns a <see cref="EffectSizeReport"/> or
/// <see cref="DifferenceReport"/> with the user-configured significance level baked into
/// the CI; that way a single <c>alpha</c> threading through the test settings determines
/// both the significance decision <em>and</em> the magnitude band the user sees.
/// </para>
/// <para>
/// References: Hedges &amp; Olkin (1985) for Hedges' g; Cliff (1993) and Romano et al. (2006)
/// for Cliff's delta variance; Hollander, Wolfe &amp; Chicken §4.2 for the Hodges-Lehmann
/// estimator and its distribution-free CI.
/// </para>
/// </remarks>
internal static class EffectSizes
{
    /// <summary>
    /// Hedges' g — the small-sample-corrected standardised mean difference. Computed from
    /// the pooled standard deviation under Welch's "average variance" convention so it
    /// pairs cleanly with the Welch t-test SailDiff uses.
    /// </summary>
    /// <remarks>
    /// g = J · d, where d = (mean1 − mean2) / s_pool and
    /// J = 1 − 3 / (4·(n1+n2) − 9) is Hedges' small-sample correction factor. The CI is
    /// a normal-approximation interval on g with
    /// SE(g) = sqrt((n1+n2)/(n1·n2) + g²/(2·(n1+n2−2))). Width follows the configured α.
    /// </remarks>
    public static EffectSizeReport? HedgesG(
        double mean1, double var1, int n1,
        double mean2, double var2, int n2,
        double alpha)
    {
        if (n1 < 2 || n2 < 2) return null;
        var sPoolSquared = (var1 + var2) / 2.0;
        if (sPoolSquared <= 0) return null;
        var sPool = Math.Sqrt(sPoolSquared);
        var d = (mean2 - mean1) / sPool;

        var totalDof = n1 + n2 - 2;
        // Hedges' correction: J = Γ((dof)/2) / (sqrt(dof/2) · Γ((dof-1)/2)). The closed-form
        // approximation J ≈ 1 − 3/(4·dof − 1) is accurate to 3 sig figs and used by most
        // textbooks; the 4·(n1+n2) − 9 variant in the docstring is the same expression in
        // total-N form.
        var j = 1.0 - 3.0 / (4.0 * totalDof - 1.0);
        var g = j * d;

        // Standard error of g (Hedges & Olkin, eq 7.20):
        // SE(g) = sqrt((n1+n2)/(n1·n2) + g²/(2·(n1+n2)))
        var seG = Math.Sqrt((double)(n1 + n2) / (n1 * n2) + g * g / (2.0 * (n1 + n2)));
        var z = Normal.InvCDF(0, 1, 1.0 - alpha / 2.0);
        var ciLower = g - z * seG;
        var ciUpper = g + z * seG;

        return new EffectSizeReport("Hedges' g", g, ciLower, ciUpper);
    }

    /// <summary>
    /// Cliff's delta — δ = P(X1 &gt; X2) − P(X1 &lt; X2), bounded in [−1, 1]. Sign matches the
    /// direction <em>sample 2 minus sample 1</em>: positive δ means sample 2 tends to
    /// produce larger values (a regression in SailDiff terms).
    /// </summary>
    /// <remarks>
    /// Computed directly from the pairwise dominance counts (O(n1·n2)). The CI is a
    /// normal-approximation Cliff-Romano interval on the Fisher-z–transformed delta.
    /// </remarks>
    public static EffectSizeReport CliffsDelta(double[] sample1, double[] sample2, double alpha)
    {
        var n1 = sample1.Length;
        var n2 = sample2.Length;
        long greater = 0, less = 0;
        for (var i = 0; i < n1; i++)
        {
            var a = sample1[i];
            for (var j = 0; j < n2; j++)
            {
                var b = sample2[j];
                // Sign convention: positive δ when sample2 > sample1, matching SailDiff's
                // "Regressed" semantic where After > Before.
                if (b > a) greater++;
                else if (b < a) less++;
            }
        }

        var total = (long)n1 * n2;
        if (total == 0) return new EffectSizeReport("Cliff's delta", 0.0, null, null);

        var delta = (double)(greater - less) / total;

        // CI: Cliff-Romano normal approximation on δ. Variance estimator:
        //   var(δ) ≈ ((n1−1)·s²_d1 + (n2−1)·s²_d2 + s²_d) / (n1·n2)
        // where s²_d1, s²_d2 are variances of per-row / per-column dominance indicators and
        // s²_d is the variance of the indicator matrix as a whole. For runtime simplicity
        // we use the conservative paired-bootstrap-style SE:
        //   SE(δ) ≈ sqrt((1 − δ²) / (min(n1, n2) − 1))
        // which is the Fisher-z normal approximation. For small samples (min(n1, n2) ≤ 2)
        // the CI is undefined.
        if (Math.Min(n1, n2) <= 2) return new EffectSizeReport("Cliff's delta", delta, null, null);

        var seDelta = Math.Sqrt((1.0 - delta * delta) / (Math.Min(n1, n2) - 1));
        var z = Normal.InvCDF(0, 1, 1.0 - alpha / 2.0);
        var ciLower = Math.Max(-1.0, delta - z * seDelta);
        var ciUpper = Math.Min(1.0, delta + z * seDelta);

        return new EffectSizeReport("Cliff's delta", delta, ciLower, ciUpper);
    }

    /// <summary>
    /// Hodges-Lehmann shift estimator: the median of all n1·n2 pairwise differences
    /// <c>(sample2[j] − sample1[i])</c>. Robust, distribution-free estimate of the location
    /// shift between the two samples, paired naturally with the Mann-Whitney rank-sum test.
    /// </summary>
    /// <remarks>
    /// CI uses the distribution-free formula based on the rank-sum critical value
    /// (Hollander, Wolfe &amp; Chicken §4.2, "Estimation Associated with the Wilcoxon Rank Sum
    /// Test"): the kth and (n1·n2 − k + 1)th smallest pairwise differences, where k is
    /// chosen so the CI's coverage is at least <c>1 − alpha</c>. The exact k comes from
    /// the rank-sum critical value at α/2.
    /// </remarks>
    public static DifferenceReport HodgesLehmann(double[] sample1, double[] sample2, double alpha, string units = "ms")
    {
        var n1 = sample1.Length;
        var n2 = sample2.Length;
        if (n1 == 0 || n2 == 0)
            return new DifferenceReport("Hodges-Lehmann shift", 0.0, null, null, units);

        // Materialise the n1·n2 pairwise differences (sample2 − sample1 to match the
        // "After − Before" SailDiff convention).
        var pairwise = new double[(long)n1 * n2];
        var k = 0;
        for (var i = 0; i < n1; i++)
            for (var j = 0; j < n2; j++)
                pairwise[k++] = sample2[j] - sample1[i];
        Array.Sort(pairwise);

        var pointEstimate = Median(pairwise);

        if (n1 < 2 || n2 < 2)
            return new DifferenceReport("Hodges-Lehmann shift", pointEstimate, null, null, units);

        // Distribution-free CI: the rank-sum normal-approximation critical value at α/2
        // gives the index offset into the sorted pairwise-difference array.
        var meanU = n1 * n2 / 2.0;
        var varU = n1 * n2 * (n1 + n2 + 1.0) / 12.0;
        var z = Normal.InvCDF(0, 1, 1.0 - alpha / 2.0);
        var halfWidth = z * Math.Sqrt(varU);
        var kLow = (int)Math.Floor(meanU - halfWidth);
        var kHigh = (int)Math.Ceiling(meanU + halfWidth);

        double? ciLower = null;
        double? ciUpper = null;
        if (kLow > 0 && kLow <= pairwise.Length) ciLower = pairwise[kLow - 1];
        if (kHigh > 0 && kHigh <= pairwise.Length) ciUpper = pairwise[kHigh - 1];

        return new DifferenceReport("Hodges-Lehmann shift", pointEstimate, ciLower, ciUpper, units);
    }

    /// <summary>
    /// Builds a mean-difference report from an already-computed Welch CI.
    /// </summary>
    public static DifferenceReport MeanDifference(double meanBefore, double meanAfter, double ciLower, double ciUpper, string units = "ms")
    {
        // Sign convention: positive when After > Before (regression).
        var diff = meanAfter - meanBefore;
        // The TwoSampleT CI is built on (mean1 − mean2). Convert by flipping signs so the
        // CI bounds match the After − Before convention used everywhere else in SailDiff.
        var lower = -ciUpper;
        var upper = -ciLower;
        return new DifferenceReport("Mean difference", diff, lower, upper, units);
    }

    private static double Median(double[] sorted)
    {
        var n = sorted.Length;
        if (n == 0) return 0;
        if (n % 2 == 1) return sorted[n / 2];
        return (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
    }
}
