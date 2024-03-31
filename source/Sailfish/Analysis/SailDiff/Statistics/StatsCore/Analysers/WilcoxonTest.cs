using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

public class WilcoxonTest : HypothesisTest<WilcoxonDistribution>
{
    private int[] Signs { get; set; }

    private double[] Delta { get; set; }

    private double[] Ranks { get; set; }

    private bool HasZeros { get; set; }

    public override WilcoxonDistribution StatisticDistribution { get; set; }

    protected void Compute(
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
        Ranks = Delta.Rank(out var _, adjustForTies: adjustForTies);
        Compute(WilcoxonDistribution.WPositive(Signs, Ranks), Ranks, tail, exact);
    }

    private void Compute(double statistic, double[] ranks, DistributionTailSailfish tail, bool? exact)
    {
        if (HasZeros)
        {
            exact ??= false;
            var nullable = exact;
            const bool flag = true;
            if ((nullable.GetValueOrDefault() == flag ? 1 : 0) != 0)
                throw new ArgumentException("An exact test cannot be computed when there are zeros in the samples (or when paired samples are the same in a paired test).");
        }

        Statistic = statistic;
        Tail = tail;
        if (ranks.Length != 0)
        {
            StatisticDistribution = new WilcoxonDistribution(ranks, exact)
            {
                Correction = Tail == DistributionTailSailfish.TwoTail ? ContinuityCorrection.Midpoint : ContinuityCorrection.KeepInside
            };
        }
        else
        {
            StatisticDistribution = null;
        }

        PValue = StatisticToPValue(Statistic);
    }

    public override double StatisticToPValue(double x)
    {
        if (StatisticDistribution == null)
            return double.NaN;
        return Tail switch
        {
            DistributionTailSailfish.TwoTail => Math.Min(2.0 * Math.Min(StatisticDistribution.DistributionFunction(x), StatisticDistribution.ComplementaryDistributionFunction(x)), 1.0),
            DistributionTailSailfish.OneUpper => StatisticDistribution.ComplementaryDistributionFunction(x),
            DistributionTailSailfish.OneLower => StatisticDistribution.DistributionFunction(x),
            _ => throw new InvalidOperationException()
        };
    }

    public override double PValueToStatistic(double p)
    {
        return Tail switch
        {
            DistributionTailSailfish.TwoTail => StatisticDistribution.InverseDistributionFunction(1.0 - p / 2.0),
            DistributionTailSailfish.OneUpper => StatisticDistribution.InverseDistributionFunction(1.0 - p),
            DistributionTailSailfish.OneLower => StatisticDistribution.InverseDistributionFunction(p),
            _ => throw new InvalidOperationException()
        };
    }
}