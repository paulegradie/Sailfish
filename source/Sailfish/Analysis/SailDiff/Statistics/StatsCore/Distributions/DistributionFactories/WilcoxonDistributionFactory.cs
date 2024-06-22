using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionFactories;

internal static class WilcoxonDistributionFactory
{
    public static WilcoxonDistribution Create(double[] ranks)
    {
        var numberOfSamples = ranks.Length;
        if (numberOfSamples == 0) throw new ArgumentOutOfRangeException(nameof(ranks), "The number of samples must be positive.");

        return new WilcoxonDistribution(ranks, ranks.Length < 12);
    }
}