using System;
using MathNet.Numerics.Distributions;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.SailDiff.Statistics;

/// <summary>
/// TOST (two one-sided tests) equivalence testing on log-time.
/// </summary>
/// <remarks>
/// <para>
/// A non-significant difference test never demonstrates similarity — it conflates "the samples are
/// equivalent" with "the run was too noisy to tell". TOST makes the equivalence claim testable: pick
/// a margin m (e.g. 5%), define the equivalence band on the ratio scale as
/// [1/(1+m/100), 1+m/100], and run two one-sided Welch t-tests on log(time) — one against each band
/// edge. When both reject at α, the data demonstrate the true ratio lies inside the band
/// (Schuirmann 1987). Running each one-sided test at α keeps the overall type-I error ≤ α; no
/// multiplicity correction is needed between the two sides.
/// </para>
/// <para>
/// The log scale makes the band symmetric (±log(1+m/100)) and matches how benchmark changes are
/// reported (ratios, not absolute milliseconds). Because logs require strictly positive samples,
/// the test abstains (returns null) when either sample contains a non-positive value — as it does
/// for degenerate inputs (fewer than 2 observations per side, or zero variance on both sides).
/// </para>
/// </remarks>
public static class EquivalenceTest
{
    /// <summary>
    /// Runs the log-scale TOST on two independent samples.
    /// </summary>
    /// <param name="sample1">Baseline ("before") sample. Must be strictly positive.</param>
    /// <param name="sample2">Contender ("after") sample. Must be strictly positive.</param>
    /// <param name="marginPercent">Equivalence margin in percent; must be &gt; 0.</param>
    /// <param name="alpha">Significance level for each one-sided test.</param>
    /// <returns>The TOST result, or null when the test is not computable for these inputs.</returns>
    public static EquivalenceTestResult? LogScaleTost(double[] sample1, double[] sample2, double marginPercent, double alpha)
    {
        if (sample1 is null || sample2 is null) return null;
        if (sample1.Length < 2 || sample2.Length < 2) return null;
        if (!(marginPercent > 0)) return null;
        if (!(alpha > 0 && alpha < 1)) return null;
        if (!AllStrictlyPositive(sample1) || !AllStrictlyPositive(sample2)) return null;

        var (mean1, var1) = LogMeanAndVariance(sample1);
        var (mean2, var2) = LogMeanAndVariance(sample2);

        // Welch machinery on the log scale.
        var n1 = sample1.Length;
        var n2 = sample2.Length;
        var se = Math.Sqrt(var1 / n1 + var2 / n2);
        if (!(se > 0) || !double.IsFinite(se)) return null;

        var num1 = var1 / n1;
        var num2 = var2 / n2;
        var degreesOfFreedom = (num1 + num2) * (num1 + num2) / (num1 * num1 / (n1 - 1) + num2 * num2 / (n2 - 1));
        if (!double.IsFinite(degreesOfFreedom) || degreesOfFreedom < 1) degreesOfFreedom = 1;

        // log-ratio (after / before); the band edge in log space.
        var diff = mean2 - mean1;
        var delta = Math.Log(1.0 + marginPercent / 100.0);

        // H0a: diff ≥ +δ. Rejected when diff is sufficiently below the upper edge — lower tail.
        var tLower = (diff - delta) / se;
        var pLower = StudentT.CDF(0, 1, degreesOfFreedom, tLower);

        // H0b: diff ≤ −δ. Rejected when diff is sufficiently above the lower edge — upper tail,
        // evaluated as CDF(−t) so extreme statistics keep their tiny-but-positive tail instead of
        // collapsing to 0 through 1 − CDF(t) (same cancellation hazard LogRatioPValue guards).
        var tUpper = (diff + delta) / se;
        var pUpper = StudentT.CDF(0, 1, degreesOfFreedom, -tUpper);

        if (double.IsNaN(pLower) || double.IsNaN(pUpper)) return null;

        var pValue = Math.Max(pLower, pUpper);
        var isEquivalent = pValue <= alpha;

        return new EquivalenceTestResult(marginPercent, pLower, pUpper, pValue, isEquivalent);
    }

    private static bool AllStrictlyPositive(double[] sample)
    {
        for (var i = 0; i < sample.Length; i++)
            if (!(sample[i] > 0) || !double.IsFinite(sample[i]))
                return false;
        return true;
    }

    private static (double Mean, double Variance) LogMeanAndVariance(double[] sample)
    {
        double sum = 0;
        for (var i = 0; i < sample.Length; i++) sum += Math.Log(sample[i]);
        var mean = sum / sample.Length;

        double sqSum = 0;
        for (var i = 0; i < sample.Length; i++)
        {
            var d = Math.Log(sample[i]) - mean;
            sqSum += d * d;
        }

        var variance = sample.Length > 1 ? sqSum / (sample.Length - 1) : 0.0;
        return (mean, variance);
    }
}
