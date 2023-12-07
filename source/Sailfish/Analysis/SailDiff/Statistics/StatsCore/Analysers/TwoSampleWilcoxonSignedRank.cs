using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;
using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

/// <summary>Wilcoxon signed-rank test for paired samples.</summary>
/// <seealso cref="T:Accord.Statistics.Testing.WilcoxonSignedRankTest" />
/// <seealso cref="T:Accord.Statistics.Distributions.Univariate.WilcoxonDistribution" />
public class TwoSampleWilcoxonSignedRank : WilcoxonTest
{
    /// <summary>
    ///     Tests whether the medians of two paired samples are different.
    /// </summary>
    /// <param name="sample1">The first sample.</param>
    /// <param name="sample2">The second sample.</param>
    /// <param name="alternate">The alternative hypothesis (research hypothesis) to test.</param>
    /// <param name="exact">
    ///     True to compute the exact distribution. May require a significant
    ///     amount of processing power for large samples (n &gt; 12). If left at null, whether to
    ///     compute the exact or approximate distribution will depend on the number of samples.
    ///     Default is null.
    /// </param>
    /// <param name="adjustForTies">
    ///     Whether to account for ties when computing the
    ///     rank statistics or not. Default is true.
    /// </param>
    public TwoSampleWilcoxonSignedRank(
        double[] sample1,
        double[] sample2,
        TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent,
        bool? exact = null,
        bool adjustForTies = true)
    {
        if (sample1.Length != sample2.Length)
            throw new DimensionMismatchException(nameof(sample2), "Both samples should be of the same size.");
        var signs = new int[sample1.Length];
        var diffs = new double[sample1.Length];
        for (var index = 0; index < sample1.Length; ++index)
        {
            var num = sample1[index] - sample2[index];
            signs[index] = Math.Sign(num);
            diffs[index] = Math.Abs(num);
        }

        Hypothesis = alternate;
        Compute(signs, diffs, (DistributionTailSailfish)alternate, exact, adjustForTies);
    }

    /// <summary>
    ///     Gets the alternative hypothesis under test. If the test is
    ///     <see cref="P:Accord.Statistics.Testing.IHypothesisTest.Significant" />, the null hypothesis can be rejected
    ///     in favor of this alternative hypothesis.
    /// </summary>
    public TwoSampleHypothesis Hypothesis { get; protected set; }
}