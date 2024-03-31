using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

public static class TwoSampleTFactory
{
    public static TwoSampleT Create(
        double[] sample1,
        double[] sample2,
        bool assumeEqualVariances = true,
        double hypothesizedDifference = 0.0,
        TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent)
    {
        return new TwoSampleT(
            sample1.Mean(),
            sample1.Variance(),
            sample1.Length,
            sample2.Mean(),
            sample2.Variance(),
            sample2.Length,
            assumeEqualVariances,
            hypothesizedDifference,
            alternate);
    }
}