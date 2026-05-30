using System;
using MathNet.Numerics.Distributions;

namespace Sailfish.Analysis.SailDiff.Statistics;

/// <summary>
/// Minimum Detectable Effect (MDE) — given α, target power (1 − β), observed sample variance,
/// and sample size, the smallest effect a two-sample test could reliably detect.
/// </summary>
/// <remarks>
/// <para>
/// For two independent samples with equal variance <c>σ²</c> and equal size <c>N</c> the
/// two-sided MDE for the mean difference is
/// <c>MDE = (z_{1−α/2} + z_{1−β}) · σ · sqrt(2 / N)</c>
/// where <c>z_p</c> is the standard-normal quantile at probability <c>p</c>. This is the
/// standard sample-size / detectable-effect formula (Cohen 1988; Conover §5.1). When the
/// two samples have different sizes or variances we use the Welch-style approximation
/// <c>MDE = (z_{1−α/2} + z_{1−β}) · sqrt(var1/n1 + var2/n2)</c>.
/// </para>
/// <para>
/// Two flavours are exposed: the raw MDE in the sample's units, and the relative MDE as
/// a percentage of the pooled mean — the form benchmark users actually look at ("this run
/// could detect changes ≥ 8% at α=0.05, 80% power").
/// </para>
/// </remarks>
public static class MinimumDetectableEffect
{
    /// <summary>
    /// Conventional power target for benchmark workflows: 80% power to detect the MDE.
    /// Equivalent to a Type II error rate of 0.20.
    /// </summary>
    public const double DefaultPower = 0.80;

    /// <summary>
    /// MDE on the raw scale of the inputs — the smallest absolute mean difference the
    /// two-sample comparison could detect at the given <paramref name="alpha"/> and
    /// <paramref name="power"/>. Returns <c>null</c> when sample sizes are too small or
    /// the variance estimates are degenerate.
    /// </summary>
    /// <param name="variance1">Sample variance of group 1.</param>
    /// <param name="n1">Sample size of group 1.</param>
    /// <param name="variance2">Sample variance of group 2.</param>
    /// <param name="n2">Sample size of group 2.</param>
    /// <param name="alpha">Significance level (Type I error rate).</param>
    /// <param name="power">Desired power (1 − Type II error rate). Defaults to <see cref="DefaultPower"/>.</param>
    public static double? Absolute(
        double variance1, int n1,
        double variance2, int n2,
        double alpha,
        double power = DefaultPower)
    {
        if (n1 < 2 || n2 < 2) return null;
        if (variance1 < 0 || variance2 < 0) return null;
        if (!(alpha > 0 && alpha < 1)) return null;
        if (!(power > 0 && power < 1)) return null;

        var standardError = Math.Sqrt(variance1 / n1 + variance2 / n2);
        if (!(standardError > 0)) return null;

        // z-quantiles for the two-sided test. Using the normal approximation; for typical
        // benchmark N (≥ 10) this is accurate to ~1% versus the exact t-quantile, and the
        // MDE is itself a planning-stage estimate so the trade-off is fine.
        var zAlpha = Normal.InvCDF(0, 1, 1.0 - alpha / 2.0);
        var zBeta = Normal.InvCDF(0, 1, power);

        return (zAlpha + zBeta) * standardError;
    }

    /// <summary>
    /// MDE as a percentage of the pooled mean — the user-facing "I could detect ≥ X%
    /// changes" metric. Pooled mean is the weighted average of the two sample means.
    /// Returns <c>null</c> when the pooled mean is not strictly positive (the relative
    /// expression is undefined for zero/negative locations).
    /// </summary>
    public static double? RelativePercent(
        double mean1, double variance1, int n1,
        double mean2, double variance2, int n2,
        double alpha,
        double power = DefaultPower)
    {
        var absolute = Absolute(variance1, n1, variance2, n2, alpha, power);
        if (absolute is null) return null;
        var pooledMean = (mean1 * n1 + mean2 * n2) / (double)(n1 + n2);
        if (!(pooledMean > 0)) return null;
        return absolute.Value / pooledMean * 100.0;
    }

    /// <summary>
    /// Sample size needed (per group) to detect a given absolute effect at the given α and
    /// power, assuming the supplied per-sample variance is representative. Useful for the
    /// inverse question: "how many iterations do I need to reliably catch a 5% regression?"
    /// </summary>
    /// <remarks>
    /// Solves the equation <c>effect = (z_{1−α/2} + z_{1−β}) · sqrt(2σ²/N)</c> for <c>N</c>,
    /// rounded up to the next integer. Returns <c>null</c> for degenerate inputs.
    /// </remarks>
    public static int? SampleSizePerGroup(
        double effectAbsolute,
        double variance,
        double alpha,
        double power = DefaultPower)
    {
        if (!(effectAbsolute > 0)) return null;
        if (variance < 0) return null;
        if (!(alpha > 0 && alpha < 1)) return null;
        if (!(power > 0 && power < 1)) return null;
        if (variance == 0) return 2; // any sample size detects a non-zero effect with zero variance

        var zAlpha = Normal.InvCDF(0, 1, 1.0 - alpha / 2.0);
        var zBeta = Normal.InvCDF(0, 1, power);
        var n = 2.0 * variance * Math.Pow((zAlpha + zBeta) / effectAbsolute, 2);

        // Tiny effects relative to variance can push `n` past int.MaxValue; an unchecked
        // cast would wrap to a negative count. Saturate at int.MaxValue instead. Conversely,
        // very large effects relative to variance can give n < 2 — but a two-sample test
        // still needs at least N=2 per side, so we clamp upward to match the zero-variance
        // branch's floor.
        if (double.IsNaN(n)) return null;
        if (n >= int.MaxValue) return int.MaxValue;
        return Math.Max(2, (int)Math.Ceiling(n));
    }
}
