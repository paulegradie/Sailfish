using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionFactories;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.AnalysersBase;

internal class TwoSampleWilcoxonSignedRank : HypothesisTest
{
    public TwoSampleWilcoxonSignedRank(int[] signs, double[] ranks, DistributionTailSailfish tailType)
    {
        Statistic = WilcoxonDistribution.WPositive(signs, ranks);
        Tail = tailType;
        StatisticDistribution = WilcoxonDistributionFactory.Create(ranks);
        PValue = Math.Min(2.0 * Math.Min(StatisticDistribution.DistributionFunction(Statistic), StatisticDistribution.ComplementaryDistributionFunction(Statistic)), 1.0);
    }

    public WilcoxonDistribution StatisticDistribution { get; }
}