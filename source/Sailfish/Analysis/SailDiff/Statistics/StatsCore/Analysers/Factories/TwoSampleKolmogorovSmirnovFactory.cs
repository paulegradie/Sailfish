namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.Factories;

public static class TwoSampleKolmogorovSmirnovFactory
{
    public static TwoSampleKolmogorovSmirnov Create(
        double[] sample1,
        double[] sample2,
        TwoSampleKolmogorovSmirnovTestHypothesis alternate = TwoSampleKolmogorovSmirnovTestHypothesis.SamplesDistributionsAreUnequal)
    {
        return new TwoSampleKolmogorovSmirnov(sample1, sample2, alternate);
    }
}