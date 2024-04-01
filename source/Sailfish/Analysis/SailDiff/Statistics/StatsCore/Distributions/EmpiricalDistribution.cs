using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;
using System;
using System.Linq;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

internal sealed class EmpiricalDistribution : UnivariateContinuousDistribution
{
    private readonly double constant;
    private double? mean;
    private readonly int[]? repeats;
    private readonly double sumOfWeights;
    private readonly WeightType type;
    private readonly double[]? weights;

    public EmpiricalDistribution(double[] samples, double? smoothing, int[]? repeats = null, double[]? weights = null)
    {
        Smoothing = smoothing ?? SmoothingRule(samples, weights, repeats);
        Samples = samples;
        this.repeats = repeats;
        this.weights = weights;
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
    }

    private double[] Samples { get; }

    private int Length { get; }

    private double Smoothing { get; }

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

    public override DoubleRange Support => new(double.NegativeInfinity, double.PositiveInfinity);

    protected internal override double InnerDistributionFunction(double x)
    {
        switch (type)
        {
            case WeightType.None:
                {
                    var num = 0;
                    for (var index = 0; index < Samples.Length; ++index)
                        if (Samples[index] <= x)
                            ++num;

                    return num / (double)Length;
                }
            case WeightType.Repetition:
                {
                    var num = 0;
                    for (var index = 0; index < Samples.Length; ++index)
                        if (Samples[index] <= x)
                            num += repeats[index];

                    return num / (double)Length;
                }
        }

        if (type != WeightType.Fraction)
            throw new InvalidOperationException();
        var num1 = 0.0;
        for (var index = 0; index < Samples.Length; ++index)
            if (Samples[index] <= x)
                num1 += weights![index];

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

    public override string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(formatProvider, "Fn(x; S)");
    }

    private static double SmoothingRule(double[] observations)
    {
        return observations.StandardDeviation() * Math.Pow(4.0 / (3.0 * observations.Length), 0.2);
    }

    private static double SmoothingRule(double[] observations, double[]? weights)
    {
        var num = weights.Sum();
        return observations.WeightedStandardDeviation(weights) * Math.Pow(4.0 / (3.0 * num), 0.2);
    }

    private static double SmoothingRule(double[] observations, int[] repeats)
    {
        double num = repeats.Sum();
        return observations.WeightedStandardDeviation(repeats) * Math.Pow(4.0 / (3.0 * num), 0.2);
    }

    private static double SmoothingRule(double[] observations, double[]? weights, int[]? repeats)
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