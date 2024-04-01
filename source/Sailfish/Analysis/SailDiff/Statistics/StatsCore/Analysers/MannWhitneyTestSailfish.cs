using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.AnalysersBase;
using System;
using System.Linq;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionFactories;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

internal sealed class MannWhitneyWilcoxon : HypothesisTest
{
    public MannWhitneyWilcoxon(double[] sample1, double[] sample2)
    {
        NumberOfSamples1 = sample1.Length;
        NumberOfSamples2 = sample2.Length;
        var source = sample1.Concatenate(sample2).Rank();
        Rank1 = source.Get(0, NumberOfSamples1);
        Rank2 = source.Get(NumberOfSamples1, 0);
        RankSum1 = Rank1.Sum();
        RankSum2 = Rank2.Sum();

        Statistic1 = RankSum1 - NumberOfSamples1 * (NumberOfSamples1 + 1) / 2.0;
        Statistic2 = RankSum2 - NumberOfSamples2 * (NumberOfSamples2 + 1) / 2.0;
        Statistic = NumberOfSamples1 < NumberOfSamples2 ? Statistic1 : Statistic2;
        Tail = DistributionTailSailfish.TwoTail;

        var dist = MannWhitneyDistributionFactory.Create(Rank1, Rank2);
        Statistic = NumberOfSamples1 < NumberOfSamples2 ? Statistic1 : Statistic2;
        PValue = Math.Min(2.0 * Math.Min(dist.DistributionFunction(Statistic), dist.ComplementaryDistributionFunction(Statistic)), 1.0);
    }

    public int NumberOfSamples1 { get; }

    public int NumberOfSamples2 { get; }

    public double[] Rank1 { get; }

    public double[] Rank2 { get; }

    public double RankSum1 { get; }

    public double RankSum2 { get; }

    public double Statistic1 { get; }

    public double Statistic2 { get; }
}