using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionFactories;

internal static class WilcoxonDistributionFactory
{
    public static WilcoxonDistribution Create(double[] ranks)
    {
        var numberOfSamples = ranks.Length;
        if (numberOfSamples == 0) throw new ArgumentOutOfRangeException(nameof(ranks), "The number of samples must be positive.");

        // Always ask for the exact path; the distribution itself decides based on the cap
        // (WilcoxonDistribution.ExactMaxN) and whether the rank vector is tied. The previous
        // `ranks.Length < 12` cutoff was a defensive cap for the old combinatorial path
        // (2^11 = 2048 entries); the Tier-2 DP can comfortably handle far more.
        return new WilcoxonDistribution(ranks, exact: true);
    }
}