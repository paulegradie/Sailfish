using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Serializable]
public class NormalDistribution : UnivariateContinuousDistribution, IFormattable
{
    private double? entropy;
    private readonly bool immutable;
    private double lnconstant;
    private double mean;
    private double stdDev = 1.0;
    private double variance = 1.0;

    public NormalDistribution()
    {
        Initialize(mean, stdDev, stdDev * stdDev);
    }

    public NormalDistribution([Real] double mean)
    {
        Initialize(mean, stdDev, stdDev * stdDev);
    }

    public NormalDistribution([Real] double mean, [Positive] double stdDev)
    {
        if (stdDev <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(stdDev), "Standard deviation must be positive.");
        Initialize(mean, stdDev, stdDev * stdDev);
    }

    public virtual double StandardDeviation => stdDev;

    public void Fit(double[] observations, double[]? weights, NormalOptions? options)
    {
        if (immutable)
            throw new InvalidOperationException("NormalDistribution.Standard is immutable.");
        double num1;
        double num2;
        if (weights != null)
        {
            num1 = observations.WeightedMean(weights);
            num2 = observations.WeightedVariance(weights, num1);
        }
        else
        {
            num1 = observations.Mean();
            num2 = observations.Variance(num1);
        }

        if (options != null)
        {
            if (options.Robust)
            {
                Initialize(num1, Math.Sqrt(num2), num2);
                return;
            }

            var regularization = options.Regularization;
            if (num2 == 0.0 || double.IsNaN(num2) || double.IsInfinity(num2))
                num2 = regularization;
        }

        if (double.IsNaN(num2) || num2 <= 0.0)
            throw new ArgumentException("Variance is zero. Try specifying a regularization constant in the fitting options.");
        Initialize(num1, Math.Sqrt(num2), num2);
    }

    public override object Clone()
    {
        return new NormalDistribution(mean, stdDev);
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        return string.Format(formatProvider, "N(x; μ = {0}, σ\u00B2 = {1})", mean.ToString(format, formatProvider), variance.ToString(format, formatProvider));
    }

    public override double Mean => mean;

    public override double Median => mean;

    public override double Mode => mean;

    public override DoubleRange Support => new(double.NegativeInfinity, double.PositiveInfinity);

    public override double Entropy
    {
        get
        {
            if (!entropy.HasValue)
                entropy = 0.5 * (Math.Log(2.0 * Math.PI * variance) + 1.0);
            return entropy.Value;
        }
    }

    protected internal override double InnerDistributionFunction(double x)
    {
        return Normal.Function((x - mean) / stdDev);
    }

    protected internal override double InnerComplementaryDistributionFunction(double x)
    {
        return Normal.Complemented((x - mean) / stdDev);
    }

    protected internal override double InnerInverseDistributionFunction(double p)
    {
        return mean + stdDev * Normal.Inverse(p);
    }

    protected internal override double InnerProbabilityDensityFunction(double x)
    {
        var num = (x - mean) / stdDev;
        return Math.Exp(lnconstant - num * num * 0.5);
    }

    protected internal override double InnerLogProbabilityDensityFunction(double x)
    {
        var num = (x - mean) / stdDev;
        return lnconstant - num * num * 0.5;
    }

    private void Initialize(double mu, double dev, double var)
    {
        mean = mu;
        stdDev = dev;
        variance = var;
        lnconstant = -Math.Log(2.5066282746310007 * dev);
    }
}