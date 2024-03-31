using System;
using System.Linq;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

[Serializable]
public sealed class MannWhitneyWilcoxon : HypothesisTest
{
    public MannWhitneyWilcoxon(
        double[] sample1,
        double[] sample2,
        TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent,
        bool? exact = null,
        bool adjustForTies = true)
    {
        NumberOfSamples1 = sample1.Length;
        NumberOfSamples2 = sample2.Length;
        var source = sample1.Concatenate(sample2).Rank(out var hasTies, adjustForTies: adjustForTies);
        Rank1 = source.Get(0, NumberOfSamples1);
        Rank2 = source.Get(NumberOfSamples1, 0);
        RankSum1 = Rank1.Sum();
        RankSum2 = Rank2.Sum();
        if (hasTies)
        {
            var nullable = exact;
            const bool flag = true;
            if ((nullable.GetValueOrDefault() == flag ? nullable.HasValue ? 1 : 0 : 0) != 0)
                exact = false;
        }

        Statistic1 = RankSum1 - NumberOfSamples1 * (NumberOfSamples1 + 1) / 2.0;
        Statistic2 = RankSum2 - NumberOfSamples2 * (NumberOfSamples2 + 1) / 2.0;
        Statistic = NumberOfSamples1 < NumberOfSamples2 ? Statistic1 : Statistic2;
        Hypothesis = alternate;
        Tail = (DistributionTailSailfish)alternate;
        if (NumberOfSamples1 < NumberOfSamples2)
        {
            Statistic = Statistic1;
            StatisticDistribution = new MannWhitneyDistribution(Rank1, Rank2, exact)
            {
                Correction = Tail == DistributionTailSailfish.TwoTail ? ContinuityCorrection.Midpoint : ContinuityCorrection.KeepInside
            };
        }
        else
        {
            Statistic = Statistic2;
            StatisticDistribution = new MannWhitneyDistribution(Rank2, Rank1, exact)
            {
                Correction = Tail == DistributionTailSailfish.TwoTail ? ContinuityCorrection.Midpoint : ContinuityCorrection.KeepInside
            };
        }

        IsExact = StatisticDistribution.Exact;
        PValue = StatisticToPValue(Statistic);
    }

    public TwoSampleHypothesis Hypothesis { get; protected set; }

    public int NumberOfSamples1 { get; protected set; }

    public int NumberOfSamples2 { get; protected set; }

    public double[] Rank1 { get; protected set; }

    public double[] Rank2 { get; protected set; }

    public double RankSum1 { get; protected set; }

    public double RankSum2 { get; protected set; }

    public double Statistic1 { get; protected set; }

    public double Statistic2 { get; protected set; }

    public bool IsExact { get; private set; }
    public MannWhitneyDistribution StatisticDistribution { get; set; }

    public double StatisticToPValue(double x)
    {
        return Tail switch
        {
            DistributionTailSailfish.TwoTail => Math.Min(2.0 * Math.Min(StatisticDistribution.DistributionFunction(x), StatisticDistribution.ComplementaryDistributionFunction(x)), 1.0),
            DistributionTailSailfish.OneUpper => NumberOfSamples1 < NumberOfSamples2
                ? StatisticDistribution.ComplementaryDistributionFunction(x)
                : StatisticDistribution.DistributionFunction(x),
            DistributionTailSailfish.OneLower => NumberOfSamples1 < NumberOfSamples2
                ? StatisticDistribution.DistributionFunction(x)
                : StatisticDistribution.ComplementaryDistributionFunction(x),
            _ => throw new InvalidOperationException()
        };
    }

    public double PValueToStatistic(double p)
    {
        throw new NotImplementedException("This method has not been implemented yet. Please open an issue in the project issue tracker if you are interested in this feature.");
    }
}