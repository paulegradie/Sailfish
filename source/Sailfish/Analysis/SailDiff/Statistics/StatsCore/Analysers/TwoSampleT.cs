using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.AnalysersBase;
using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

internal sealed class TwoSampleT : HypothesisTest
{
    public TwoSampleT(
        double mean1,
        double var1,
        int samples1,
        double mean2,
        double var2,
        int samples2,
        bool assumeEqualVariances = true,
        double hypothesizedDifference = 0.0,
        TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent)
    {
        AssumeEqualVariance = assumeEqualVariances;
        EstimatedValue1 = mean1;
        EstimatedValue2 = mean2;
        double degreesOfFreedom;
        if (AssumeEqualVariance)
        {
            Variance = ((samples1 - 1) * var1 + (samples2 - 1) * var2) / (samples1 + samples2 - 2);
            StandardError = Math.Sqrt(Variance) * Math.Sqrt(1.0 / samples1 + 1.0 / samples2);
            degreesOfFreedom = samples1 + samples2 - 2;
        }
        else
        {
            StandardError = Math.Sqrt(var1 / samples1 + var2 / samples2);
            Variance = (var1 + var2) / 2.0;
            var num1 = var1 / samples1;
            var num2 = var2 / samples2;
            degreesOfFreedom = (num1 + num2) * (num1 + num2) / (num1 * num1 / (samples1 - 1) + num2 * num2 / (samples2 - 1));
        }

        ObservedDifference = mean1 - mean2;
        HypothesizedDifference = hypothesizedDifference;
        Statistic = (ObservedDifference - HypothesizedDifference) / StandardError;
        StatisticDistribution = new Distribution(degreesOfFreedom);
        Hypothesis = alternate;
        Tail = (DistributionTailSailfish)alternate;
        PValue = StatisticToPValue(Statistic, StatisticDistribution, Tail);
        Confidence = GetConfidenceInterval(1.0 - Size);
    }

    public TwoSampleHypothesis Hypothesis { get; private set; }

    public bool AssumeEqualVariance { get; private set; }

    public double StandardError { get; }

    public double Variance { get; }

    public double EstimatedValue1 { get; }

    public double EstimatedValue2 { get; }

    public double HypothesizedDifference { get; }

    public double ObservedDifference { get; }

    public double DegreesOfFreedom => StatisticDistribution.DegreesOfFreedom;

    public DoubleRange Confidence { get; set; }
    private Distribution StatisticDistribution { get; }

    public DoubleRange GetConfidenceInterval(double percent = 0.95)
    {
        var statistic = PValueToStatistic(1.0 - percent, StatisticDistribution, Tail);
        return new DoubleRange(ObservedDifference - statistic * StandardError, ObservedDifference + statistic * StandardError);
    }

    public static double StatisticToPValue(double t, Distribution distribution, DistributionTailSailfish type)
    {
        return type switch
        {
            DistributionTailSailfish.TwoTail => 2.0 * distribution.ComplementaryDistributionFunction(Math.Abs(t)),
            DistributionTailSailfish.OneUpper => distribution.ComplementaryDistributionFunction(t),
            DistributionTailSailfish.OneLower => distribution.DistributionFunction(t),
            _ => throw new InvalidOperationException()
        };
    }

    public static double PValueToStatistic(double p, Distribution distribution, DistributionTailSailfish type)
    {
        return type switch
        {
            DistributionTailSailfish.TwoTail => distribution.InverseDistributionFunction(1.0 - p / 2.0),
            DistributionTailSailfish.OneUpper => distribution.InverseDistributionFunction(1.0 - p),
            DistributionTailSailfish.OneLower => distribution.InverseDistributionFunction(p),
            _ => throw new InvalidOperationException()
        };
    }
}