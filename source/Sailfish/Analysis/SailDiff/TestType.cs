namespace Sailfish.Analysis.SailDiff;

/// <summary>
/// Statistical test used by SailDiff to decide whether two samples differ.
/// </summary>
/// <remarks>
/// The enum order is preserved for backward compatibility — callers serialise these by name.
/// New code should prefer <see cref="WilcoxonRankSumTest"/> (the default) for independent-sample
/// benchmark comparisons. <see cref="TwoSampleWilcoxonSignedRankTest"/> is retained but is only
/// statistically valid for paired data, which independent benchmark iterations are not.
/// </remarks>
public enum TestType
{
    /// <summary>
    /// Two-sample Wilcoxon signed-rank test. <strong>Requires paired samples</strong> — each
    /// observation in "before" is paired with a specific observation in "after" by experimental
    /// design (same subject, same input, same iteration). Sample sizes must match exactly.
    /// Independent benchmark iterations are <strong>not</strong> paired; using this test on
    /// independent samples violates the test's assumptions and produces invalid p-values.
    /// Prefer <see cref="WilcoxonRankSumTest"/> for almost all SailDiff use cases.
    /// </summary>
    TwoSampleWilcoxonSignedRankTest,

    /// <summary>
    /// Wilcoxon Rank-Sum / Mann-Whitney U test. The correct non-parametric test for comparing
    /// two <strong>independent</strong> samples — i.e., the typical SailDiff scenario where
    /// "before" and "after" are separate benchmark runs. Does not assume normality; robust to
    /// the positive skew common in timing data. This is the default and recommended choice.
    /// </summary>
    WilcoxonRankSumTest,

    /// <summary>
    /// Welch's two-sample t-test (no equal-variance assumption). Parametric — assumes each
    /// sample mean is approximately normally distributed (true asymptotically by the CLT, and
    /// reasonable for log-transformed timing data even with small N). Use when you want a CI
    /// on the mean difference; prefer <see cref="WilcoxonRankSumTest"/> when sample sizes are
    /// small or the data is heavy-tailed.
    /// </summary>
    Test,

    /// <summary>
    /// Two-sample Kolmogorov-Smirnov test. Compares full empirical distributions — sensitive
    /// to differences in shape, scale, or location anywhere in the distribution. Less powerful
    /// than rank-sum for pure location shifts; use when you suspect distributional changes
    /// (e.g., a new code path with bimodal latency) rather than a simple "is run B faster?".
    /// </summary>
    KolmogorovSmirnovTest,

    /// <summary>
    /// Permutation test on the difference in means. Approximate-exact: shuffles the joint
    /// sample-1 / sample-2 labels <c>K</c> times (default 10,000) and counts how often a
    /// random permutation produces a mean difference at least as extreme as the observed
    /// one. Distribution-free, no parametric assumption — robust against heavy-tailed and
    /// multi-modal timing data where the t-test's normality assumption is shaky. Slower
    /// than the analytic tests by roughly a factor of <c>K / N</c>; use when the data has
    /// visible outliers you can't justify removing.
    /// </summary>
    PermutationTest
}
