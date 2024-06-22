using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionFactories;

namespace Tests.Library.Analysis.SailDiff;

public static class TestDistributions
{
    public static double[] GenerateRandomNormalDistribution(int sampleSize, double mean, double standardDeviation, Random random)
    {
        return NormalDistributionFactory.Create(mean, standardDeviation).Generate(sampleSize, random);
    }
}