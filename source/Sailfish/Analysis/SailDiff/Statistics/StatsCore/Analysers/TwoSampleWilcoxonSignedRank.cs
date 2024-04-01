using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.AnalysersBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

internal sealed class TwoSampleWilcoxonSignedRank : WilcoxonTest
{
    public TwoSampleWilcoxonSignedRank(
        int[] signs,
        double[] diffs,
        TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent,
        bool? exact = null,
        bool adjustForTies = true) : base(signs, diffs, (DistributionTailSailfish)alternate, exact, adjustForTies)
    {
        Hypothesis = alternate;
    }

    public TwoSampleHypothesis Hypothesis { get; private set; }
}