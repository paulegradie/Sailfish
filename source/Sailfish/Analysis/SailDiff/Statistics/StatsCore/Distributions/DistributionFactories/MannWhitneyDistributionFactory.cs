using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;
using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionFactories;

internal static class MannWhitneyDistributionFactory
{
    public static MannWhitneyDistribution Create(double[] ranks1, double[] ranks2, ContinuityCorrection continuityCorrection)
    {
        var ranks = ranks1.Concatenate(ranks2);
        var n1 = ranks1.Length;
        var n2 = ranks.Length;
        if (n1 <= 0)
            throw new ArgumentOutOfRangeException(nameof(n1), "The number of observations in the first sample (n1) must be higher than zero.");
        if (n2 <= 0)
            throw new ArgumentOutOfRangeException(nameof(n2), "The number of observations in the second sample (n2) must be higher than zero.");

        if (ranks.Length <= 1)
            throw new ArgumentOutOfRangeException(nameof(ranks), "The rank vector must contain a minimum of 2 elements.");
        for (var index = 0; index < ranks.Length; ++index)
            if (ranks[index] < 0.0)
                throw new ArgumentOutOfRangeException(nameof(index), "The rank values cannot be negative.");


        return new MannWhitneyDistribution(ranks, n1, n2, continuityCorrection);
    }
}