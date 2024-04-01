using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.AnalysersBase;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.Factories;

internal static class MannWhitneyWilcoxonFactory
{
    public static MannWhitneyWilcoxon Create(
        double[] sample1,
        double[] sample2,
        TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent,
        bool adjustForTies = true)
    {
        return new MannWhitneyWilcoxon(sample1, sample2, alternate, adjustForTies);
    }
}