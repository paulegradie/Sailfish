using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

public class WilcoxonTest : HypothesisTest<WilcoxonDistribution>
{
    private bool hasTies;

    protected WilcoxonTest()
    {
    }

    public int[] Signs { get; protected set; }

    public double[] Delta { get; protected set; }

    public double[] Ranks { get; protected set; }

    public bool HasZeros { get; private set; }

    public bool IsExact { get; private set; }
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
        Ranks = Delta.Rank(out hasTies, adjustForTies: adjustForTies);
        Compute(WilcoxonDistribution.WPositive(Signs, Ranks), Ranks, tail, exact);
    }

    protected void Compute(double statistic, double[] ranks, DistributionTailSailfish tail, bool? exact)
    {
        if (HasZeros)
        {
            if (!exact.HasValue)
                exact = false;
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
            IsExact = StatisticDistribution.Exact;
        }
        else
        {
            StatisticDistribution = null;
            IsExact = exact.GetValueOrDefault(false);
        }

        PValue = StatisticToPValue(Statistic);
        OnSizeChanged();
    }

    public override double StatisticToPValue(double x)
    {
        if (StatisticDistribution == null)
            return double.NaN;
        switch (Tail)
        {
            case DistributionTailSailfish.TwoTail:
                return Math.Min(2.0 * Math.Min(StatisticDistribution.DistributionFunction(x), StatisticDistribution.ComplementaryDistributionFunction(x)), 1.0);

            case DistributionTailSailfish.OneUpper:
                return StatisticDistribution.ComplementaryDistributionFunction(x);

            case DistributionTailSailfish.OneLower:
                return StatisticDistribution.DistributionFunction(x);

            default:
                throw new InvalidOperationException();
        }
    }

    public override double PValueToStatistic(double p)
    {
        switch (Tail)
        {
            case DistributionTailSailfish.TwoTail:
                return StatisticDistribution.InverseDistributionFunction(1.0 - p / 2.0);

            case DistributionTailSailfish.OneUpper:
                return StatisticDistribution.InverseDistributionFunction(1.0 - p);

            case DistributionTailSailfish.OneLower:
                return StatisticDistribution.InverseDistributionFunction(p);

            default:
                throw new InvalidOperationException();
        }
    }
}