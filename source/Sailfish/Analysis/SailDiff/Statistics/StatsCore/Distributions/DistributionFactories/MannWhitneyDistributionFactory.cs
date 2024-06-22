using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionFactories;

internal static class MannWhitneyDistributionFactory
{
    public static MannWhitneyDistribution Create(double[] ranks1, double[] ranks2)
    {
        var ranks = ranks1.Concatenate(ranks2);
        var rank1Length = ranks1.Length;
        var rank2Length = ranks2.Length;
        if (rank1Length <= 0) throw new ArgumentOutOfRangeException(nameof(rank1Length), "The number of observations in the first sample (n1) must be higher than zero.");

        if (rank2Length <= 0) throw new ArgumentOutOfRangeException(nameof(rank2Length), "The number of observations in the second sample (n2) must be higher than zero.");

        if (ranks.Length <= 1) throw new ArgumentOutOfRangeException(nameof(ranks), "The rank vector must contain a minimum of 2 elements.");

        for (var index = 0; index < ranks.Length; ++index)
            if (ranks[index] < 0.0)
                throw new ArgumentOutOfRangeException(nameof(index), "The rank values cannot be negative.");


        return new MannWhitneyDistribution(ranks, rank1Length, rank2Length);
    }
}