using System;
using System.Linq;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.AnalysersBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

internal sealed class TwoSampleKolmogorovSmirnov : HypothesisTest
{
    public TwoSampleKolmogorovSmirnov(
        double[] sample1,
        double[] sample2,
        TwoSampleKolmogorovSmirnovTestHypothesis alternate = TwoSampleKolmogorovSmirnovTestHypothesis.SamplesDistributionsAreUnequal)
    {
        Hypothesis = alternate;
        var length1 = sample1.Length;
        var length2 = sample2.Length;
        StatisticDistribution = new KolmogorovSmirnovDistribution(length1 * length2 / (double)(length1 + length2));
        EmpiricalDistribution1 = new EmpiricalDistribution(sample1, 0.0);
        EmpiricalDistribution2 = new EmpiricalDistribution(sample2, 0.0);
        var array = new double[length1 + length2 + 1];
        var values = new double[array.Length];
        array[0] = double.NegativeInfinity;
        for (var index = 0; index < sample1.Length; ++index)
            array[index + 1] = sample1[index];
        for (var index = 0; index < sample2.Length; ++index)
            array[index + length1 + 1] = sample2[index];
        Array.Sort(array);
        var func1 = EmpiricalDistribution1.DistributionFunction;
        var func2 = EmpiricalDistribution2.DistributionFunction;
        switch (alternate)
        {
            case TwoSampleKolmogorovSmirnovTestHypothesis.SamplesDistributionsAreUnequal:
                for (var index = 0; index < array.Length - 1; ++index)
                    values[index] = Math.Max(Math.Abs(func1(array[index]) - func2(array[index + 1])), Math.Abs(func1(array[index]) - func2(array[index])));
                Statistic = values.Max<double>();
                PValue = StatisticDistribution.ComplementaryDistributionFunction(Statistic);
                break;

            case TwoSampleKolmogorovSmirnovTestHypothesis.FirstSampleIsLargerThanSecond:
                for (var index = 0; index < array.Length - 1; ++index)
                    values[index] = Math.Max(func1(array[index]) - func2(array[index + 1]), func1(array[index]) - func2(array[index]));
                Statistic = values.Max<double>();
                PValue = StatisticDistribution.OneSideDistributionFunction(Statistic);
                break;

            default:
                for (var index = 0; index < array.Length - 1; ++index)
                    values[index] = Math.Max(func2(array[index + 1]) - func1(array[index]), func2(array[index]) - func1(array[index]));
                Statistic = values.Max<double>();
                PValue = StatisticDistribution.OneSideDistributionFunction(Statistic);
                break;
        }

        Tail = (DistributionTailSailfish)alternate;
    }

    public TwoSampleKolmogorovSmirnovTestHypothesis Hypothesis { get; private set; }

    public EmpiricalDistribution EmpiricalDistribution1 { get; }

    public EmpiricalDistribution EmpiricalDistribution2 { get; }
    public KolmogorovSmirnovDistribution StatisticDistribution { get; set; }
}