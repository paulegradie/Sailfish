using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionFactories;

internal static class NormalDistributionFactory
{
    public static NormalDistribution Create(double mean, double stdDev)
    {
        if (stdDev <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(stdDev), "Standard deviation must be positive.");

        return new NormalDistribution(mean, stdDev);
    }
}