using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;
using System;
using System.Linq;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class EmpiricalDistribution :
    UnivariateContinuousDistribution,
    IFittableDistribution<double, EmpiricalOptions>,
    IRandomNumberGenerator<double>
{
    private double constant;
    private double? entropy;
    private double? mean;
    private double? mode;
    private int[] repeats;
    private double sumOfWeights;
    private WeightType type;
    private double? variance;
    private double[] weights;

    public EmpiricalDistribution(double[] samples)
    {
        Initialize(samples, null, null, new double?());
    }

    public EmpiricalDistribution(double[] samples, double smoothing)
    {
        Initialize(samples, null, null, smoothing);
    }

    public EmpiricalDistribution(double[] samples, double[] weights)
    {
        Initialize(samples, weights, null, new double?());
    }

    public EmpiricalDistribution(double[] samples, int[] weights)
    {
        Initialize(samples, null, weights, new double?());
    }

    public EmpiricalDistribution(double[] samples, double[] weights, double smoothing)
    {
        Initialize(samples, weights, null, smoothing);
    }

    public EmpiricalDistribution(double[] samples, int[] weights, double smoothing)
    {
        Initialize(samples, null, weights, smoothing);
    }

    private EmpiricalDistribution()
    {
    }

    public double[] Samples { get; private set; }

    public int Length { get; private set; }

    public double Smoothing { get; private set; }

    public override double Mean
    {
        get
        {
            if (!mean.HasValue)
            {
                if (type == WeightType.None)
                    mean = Samples.Mean();
                else if (type == WeightType.Repetition)
                    mean = Samples.WeightedMean(repeats);
                else if (type == WeightType.Fraction)
                    mean = Samples.WeightedMean(weights);
            }

            return mean.Value;
        }
    }

    public override double Mode
    {
        get
        {
            if (!mode.HasValue)
            {
                if (type == WeightType.None)
                    mode = Samples.Mode();
                else if (type == WeightType.Repetition)
                    mode = Samples.WeightedMode(repeats);
                else if (type == WeightType.Fraction)
                    mode = Samples.WeightedMode(weights);
            }

            return mode.Value;
        }
    }

    public override double Variance
    {
        get
        {
            if (!variance.HasValue)
            {
                if (type == WeightType.None)
                    variance = Samples.Variance();
                else if (type == WeightType.Repetition)
                    variance = Samples.WeightedVariance(repeats);
                else if (type == WeightType.Fraction)
                    variance = Samples.WeightedVariance(weights);
            }

            return variance.Value;
        }
    }

    public override double Entropy
    {
        get
        {
            if (!entropy.HasValue)
            {
                if (type == WeightType.None)
                    entropy = Samples.Entropy(ProbabilityDensityFunction);
                else if (type == WeightType.Repetition)
                    entropy = Samples.WeightedEntropy(repeats, ProbabilityDensityFunction);
                else if (type == WeightType.Fraction)
                    entropy = Samples.WeightedEntropy(weights, ProbabilityDensityFunction);
            }

            return entropy.Value;
        }
    }

    public override DoubleRange Support => new(double.NegativeInfinity, double.PositiveInfinity);

    public void Fit(double[] observations, double[] weights, EmpiricalOptions options)
    {
        var smoothing = new double?();
        var flag = false;
        if (options != null)
        {
            smoothing = options.SmoothingRule(observations, weights);
            flag = options.InPlace;
        }

        if (!flag)
        {
            observations = (double[])observations.Clone();
            if (weights != null)
                weights = (double[])weights.Clone();
        }

        Initialize(observations, weights, null, smoothing);
    }

    public override object Clone()
    {
        var empiricalDistribution = new EmpiricalDistribution();
        empiricalDistribution.type = type;
        empiricalDistribution.sumOfWeights = sumOfWeights;
        empiricalDistribution.Length = Length;
        empiricalDistribution.Smoothing = Smoothing;
        empiricalDistribution.constant = constant;
        empiricalDistribution.Samples = (double[])Samples.Clone();
        if (weights != null)
            empiricalDistribution.weights = (double[])weights.Clone();
        if (repeats != null)
            empiricalDistribution.repeats = (int[])repeats.Clone();
        return empiricalDistribution;
    }

    public override double[] Generate(int samples, double[] result, Random source)
    {
        if (weights == null)
        {
            for (var index = 0; index < samples; ++index)
                result[index] = Samples[source.Next(Samples.Length)];
            return result;
        }

        var num1 = source.NextDouble() * sumOfWeights;
        for (var index1 = 0; index1 < samples; ++index1)
        {
            var num2 = 0.0;
            for (var index2 = 0; index2 < weights.Length; ++index2)
            {
                num2 += weights[index2];
                if (num1 < num2)
                {
                    result[index1] = Samples[index2];
                    break;
                }
            }
        }

        return result;
    }

    public override double Generate(Random source)
    {
        if (weights == null)
            return Samples[source.Next(Samples.Length)];
        var num1 = source.NextDouble() * sumOfWeights;
        var num2 = 0.0;
        for (var index = 0; index < weights.Length; ++index)
        {
            num2 += weights[index];
            if (num1 < num2)
                return Samples[index];
        }

        throw new InvalidOperationException("Execution should never reach here.");
    }

    protected internal override double InnerDistributionFunction(double x)
    {
        if (type == WeightType.None)
        {
            var num = 0;
            for (var index = 0; index < Samples.Length; ++index)
                if (Samples[index] <= x)
                    ++num;

            return num / (double)Length;
        }

        if (type == WeightType.Repetition)
        {
            var num = 0;
            for (var index = 0; index < Samples.Length; ++index)
                if (Samples[index] <= x)
                    num += repeats[index];

            return num / (double)Length;
        }

        if (type != WeightType.Fraction)
            throw new InvalidOperationException();
        var num1 = 0.0;
        for (var index = 0; index < Samples.Length; ++index)
            if (Samples[index] <= x)
                num1 += weights[index];

        return num1 / sumOfWeights;
    }

    protected internal override double InnerProbabilityDensityFunction(double x)
    {
        var num1 = 0.0;
        if (type == WeightType.None)
            for (var index = 0; index < Samples.Length; ++index)
            {
                var num2 = (x - Samples[index]) / Smoothing;
                num1 += Math.Exp(-num2 * num2 * 0.5);
            }
        else if (type == WeightType.Repetition)
            for (var index = 0; index < Samples.Length; ++index)
            {
                var num3 = (x - Samples[index]) / Smoothing;
                num1 += repeats[index] * Math.Exp(-num3 * num3 * 0.5);
            }
        else if (type == WeightType.Fraction)
            for (var index = 0; index < Samples.Length; ++index)
            {
                var num4 = (x - Samples[index]) / Smoothing;
                num1 += weights[index] * Math.Exp(-num4 * num4 * 0.5);
            }

        return num1 * constant;
    }

    public override void Fit(double[] observations, double[] weights, IFittingOptions options)
    {
        Fit(observations, weights, options as EmpiricalOptions);
    }

    private void Initialize(
        double[] observations,
        double[] weights,
        int[] repeats,
        double? smoothing)
    {
        if (!smoothing.HasValue)
            smoothing = SmoothingRule(observations, weights, repeats);
        Samples = observations;
        this.weights = weights;
        this.repeats = repeats;
        Smoothing = smoothing.Value;
        if (weights != null)
        {
            type = WeightType.Fraction;
            Length = Samples.Length;
            sumOfWeights = weights.Sum();
            constant = 1.0 / (2.5066282746310007 * Smoothing);
        }
        else if (repeats != null)
        {
            type = WeightType.Repetition;
            Length = repeats.Sum();
            sumOfWeights = 1.0;
            constant = 1.0 / (2.5066282746310007 * Smoothing * Length);
        }
        else
        {
            type = WeightType.None;
            Length = Samples.Length;
            sumOfWeights = 1.0;
            constant = 1.0 / (2.5066282746310007 * Smoothing * Length);
        }

        mean = new double?();
        variance = new double?();
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        return string.Format(formatProvider, "Fn(x; S)");
    }

    public static double SmoothingRule(double[] observations)
    {
        return observations.StandardDeviation() * Math.Pow(4.0 / (3.0 * observations.Length), 0.2);
    }

    public static double SmoothingRule(double[] observations, double[] weights)
    {
        var num = weights.Sum();
        return observations.WeightedStandardDeviation(weights) * Math.Pow(4.0 / (3.0 * num), 0.2);
    }

    public static double SmoothingRule(double[] observations, int[] repeats)
    {
        double num = repeats.Sum();
        return observations.WeightedStandardDeviation(repeats) * Math.Pow(4.0 / (3.0 * num), 0.2);
    }

    public static double SmoothingRule(double[] observations, double[] weights, int[] repeats)
    {
        if (weights != null)
        {
            if (repeats != null)
                throw new ArgumentException("Either weights or repeats can be different from null.");
            return SmoothingRule(observations, weights);
        }

        if (repeats == null)
            return SmoothingRule(observations);
        if (weights != null)
            throw new ArgumentException("Either weights or repeats can be different from null.");
        return SmoothingRule(observations, repeats);
    }
}