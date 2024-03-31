using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;


internal class TwoSampleWilcoxonSignedRank : WilcoxonTest
{
    /// <summary>
    ///     Tests whether the medians of two paired samples are different.
    /// </summary>
    /// <param name="sample1">The first sample.</param>
    /// <param name="sample2">The second sample.</param>
    /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
    /// <param name="exact">
    ///     True to compute the exact distribution. May require a significant
    ///     amount of processing power for large samples (n > 12). If left at null, whether to
    ///     compute the exact or approximate distribution will depend on the number of samples.
    ///     Default is null.
    /// </param>
    /// <param name="adjustForTies">
    ///     Whether to account for ties when computing the
    ///     rank statistics or not. Default is true.
    /// </param>
    public TwoSampleWilcoxonSignedRank(
        int[] signs,
        double[] diffs,
        TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent,
        bool? exact = null,
        bool adjustForTies = true) : base(signs, diffs, (DistributionTailSailfish)alternate, exact, adjustForTies)
    {
        Hypothesis = alternate;
    }

    public TwoSampleHypothesis Hypothesis { get; protected set; }
}