using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers;

[Serializable]
public class TwoSampleT : HypothesisTest<Distribution>
{
    private readonly TwoSampleTTestPowerAnalysis powerAnalysis;

    public TwoSampleT(
        double[] sample1,
        double[] sample2,
        bool assumeEqualVariances = true,
        double hypothesizedDifference = 0.0,
        TwoSampleHypothesis alternate = TwoSampleHypothesis.ValuesAreDifferent)
        : this(sample1.Mean(), sample1.Variance(), sample1.Length,
            sample2.Mean(), sample2.Variance(), sample2.Length,
            assumeEqualVariances, hypothesizedDifference, alternate)
    {
    }

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
        PValue = StatisticToPValue(Statistic);
        OnSizeChanged();

        var num = Math.Sqrt((var1 + var2) / 2.0);
        var ttestPowerAnalysis = new TwoSampleTTestPowerAnalysis(Hypothesis)
        {
            Samples1 = samples1,
            Samples2 = samples2,
            Effect = (ObservedDifference - HypothesizedDifference) / num,
            Size = Size
        };
        powerAnalysis = ttestPowerAnalysis;
        powerAnalysis.ComputePower();
    }

    public ITwoSamplePowerAnalysis Analysis => powerAnalysis;

    public TwoSampleHypothesis Hypothesis { get; private set; }

    public bool AssumeEqualVariance { get; private set; }

    public double StandardError { get; protected set; }

    public double Variance { get; protected set; }

    public double EstimatedValue1 { get; protected set; }

    public double EstimatedValue2 { get; protected set; }

    public double HypothesizedDifference { get; protected set; }

    public double ObservedDifference { get; protected set; }

    public double DegreesOfFreedom => StatisticDistribution.DegreesOfFreedom;

    public DoubleRange Confidence { get; protected set; }
    public override Distribution StatisticDistribution { get; set; }

    public DoubleRange GetConfidenceInterval(double percent = 0.95)
    {
        var statistic = PValueToStatistic(1.0 - percent);
        return new DoubleRange(ObservedDifference - statistic * StandardError, ObservedDifference + statistic * StandardError);
    }

    protected void OnSizeChanged()
    {
        Confidence = GetConfidenceInterval(1.0 - Size);
        if (Analysis == null)
            return;
        powerAnalysis.Size = Size;
        powerAnalysis.ComputePower();
    }

    public override double PValueToStatistic(double p)
    {
        return TestExtensionMethods.PValueToStatistic(p, StatisticDistribution, Tail);
    }

    public override double StatisticToPValue(double x)
    {
        return TestExtensionMethods.StatisticToPValue(x, StatisticDistribution, Tail);
    }
}