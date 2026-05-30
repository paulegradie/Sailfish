using System;
using Sailfish;

namespace Sailfish.Analysis.SailDiff;

/// <summary>
///     Settings to use with the regression tester.
/// </summary>
public class SailDiffSettings
{
    /// <summary>
    ///     Settings to use with the regression tester.
    /// </summary>
    /// <param name="alpha">
    ///     Significance threshold (Type I error rate). The default of 0.05 matches conventional
    ///     statistical practice and aligns with the 95% confidence intervals reported alongside
    ///     each result. Use <see cref="SailfishPreset.Tight"/> (0.01) for release-gate comparisons
    ///     where false positives are expensive; use <see cref="SailfishPreset.Relaxed"/> (0.10) for
    ///     noisy CI hosts where false negatives are worse than false positives. Family-wise
    ///     control (BH-FDR) is applied across pairs in N×N method comparisons.
    /// </param>
    /// <param name="round">The number of decimal places to round to. Typical is 4.</param>
    /// <param name="useOutlierDetection">Apply Tukey-fence outlier removal to each sample before testing.</param>
    /// <param name="testType">
    ///     Statistical test to use. The default is <see cref="TestType.WilcoxonRankSumTest"/> — the
    ///     Mann-Whitney / Wilcoxon Rank-Sum test — which is the correct non-parametric test for
    ///     comparing two independent samples (e.g., separate benchmark runs). It does not assume
    ///     normality and is robust to the positive skew common in timing data.
    ///     <see cref="TestType.Test"/> uses Welch's t-test (no equal-variance assumption).
    ///     <see cref="TestType.KolmogorovSmirnovTest"/> compares full distributions and is
    ///     insensitive to pure location shifts; prefer the rank-sum test for "is run B faster
    ///     than run A". <see cref="TestType.TwoSampleWilcoxonSignedRankTest"/> requires PAIRED
    ///     samples and is not appropriate for independent benchmark runs — see its enum docs.
    /// </param>
    /// <param name="maxDegreeOfParallelism"></param>
    /// <param name="disableOrdering"></param>
    public SailDiffSettings(double alpha = 0.05,
        int round = 3,
        bool useOutlierDetection = true,
        TestType testType = TestType.WilcoxonRankSumTest,
        int maxDegreeOfParallelism = 4,
        bool disableOrdering = false)
    {
        Alpha = alpha;
        Round = round;
        UseOutlierDetection = useOutlierDetection;
        TestType = testType;
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
        DisableOrdering = disableOrdering;
    }

    public SailDiffSettings(SailfishPreset preset) : this()
    {
        ApplyPreset(preset);
    }

    public double Alpha { get; private set; }
    public int Round { get; private set; }
    public bool UseOutlierDetection { get; private set; }
    public TestType TestType { get; private set; }
    public int MaxDegreeOfParallelism { get; private set; }
    public bool DisableOrdering { get; private set; }

    public void SetAlpha(double alpha)
    {
        Alpha = alpha;
    }

    public void SetRound(int round)
    {
        Round = round;
    }

    public void DisableOutlierDetection()
    {
        UseOutlierDetection = false;
    }

    public void SetTestType(TestType testType)
    {
        TestType = testType;
    }

    public void SetMaxDegreeOfParallelism(int maxDegreeOfParallelism)
    {
        MaxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    public void SetDisableOrdering(bool disable)
    {
        DisableOrdering = disable;
    }

    private void ApplyPreset(SailfishPreset preset)
    {
        Alpha = preset switch
        {
            SailfishPreset.Default => 0.05,
            SailfishPreset.Tight => 0.01,
            SailfishPreset.Relaxed => 0.10,
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown Sailfish preset.")
        };
    }
}
