namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.Factories;

internal static class MannWhitneyWilcoxonFactory
{
    public static MannWhitneyWilcoxon Create(double[] sample1, double[] sample2)
    {
        return new MannWhitneyWilcoxon(sample1, sample2);
    }
}