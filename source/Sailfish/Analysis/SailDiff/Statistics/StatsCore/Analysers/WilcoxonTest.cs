using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

public class WilcoxonTest : HypothesisTest
{
    protected WilcoxonTest(
        int[] signs,
        double[] diffs,
        DistributionTailSailfish tail,
        bool? exact,
        bool adjustForTies)
    {
        var indexes = diffs.Find(x => x != 0.0);
        HasZeros = indexes.Length != diffs.Length;
        if (HasZeros)
        {
            signs = signs.Get(indexes);
            diffs = diffs.Get(indexes);
        }

        Signs = signs;
        Delta = diffs;
        Ranks = Delta.Rank(out _, adjustForTies: adjustForTies);
        if (Ranks.Length == 0)
        {
            throw new Exception();
        }


        if (HasZeros)
        {
            exact ??= false;
            var nullable = exact;
            const bool flag = true;
            if ((nullable.GetValueOrDefault() == flag ? 1 : 0) != 0)
                throw new ArgumentException("An exact test cannot be computed when there are zeros in the samples (or when paired samples are the same in a paired test).");
        }

        Statistic = WilcoxonDistribution.WPositive(Signs, Ranks);
        Tail = tail;
        StatisticDistribution = new WilcoxonDistribution(Ranks, exact)
        {
            Correction = Tail == DistributionTailSailfish.TwoTail ? ContinuityCorrection.Midpoint : ContinuityCorrection.KeepInside
        };


        PValue = Tail switch
        {
            DistributionTailSailfish.TwoTail => Math.Min(2.0 * Math.Min(StatisticDistribution.DistributionFunction(Statistic), StatisticDistribution.ComplementaryDistributionFunction(Statistic)), 1.0),
            DistributionTailSailfish.OneUpper => StatisticDistribution.ComplementaryDistributionFunction(Statistic),
            DistributionTailSailfish.OneLower => StatisticDistribution.DistributionFunction(Statistic),
            _ => throw new InvalidOperationException()
        };
    }

    private int[] Signs { get; set; }

    private double[] Delta { get; set; }

    private double[] Ranks { get; set; }

    private bool HasZeros { get; set; }

    public WilcoxonDistribution StatisticDistribution { get; set; }
}