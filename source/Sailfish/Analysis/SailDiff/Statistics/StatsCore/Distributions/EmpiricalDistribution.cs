using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;
using System;
using System.Linq;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

internal sealed class EmpiricalDistribution : UnivariateContinuousDistribution
{
    public EmpiricalDistribution(double[] samples, double smoothing)
    {
        Smoothing = smoothing;
        Samples = samples;
        Mean = samples.Mean();
        Length = Samples.Length;
    }

    private double DistributionConstant => 1.0 / (2.5066282746310007 * Smoothing * Length);
    private double[] Samples { get; }

    private int Length { get; }

    private double Smoothing { get; }

    public override double Mean { get; }

    public override DoubleRange Support => new(double.NegativeInfinity, double.PositiveInfinity);

    protected override double InnerDistributionFunction(double x)
    {
        return Samples.Count(t => t <= x) / (double)Length;
    }

    protected override double InnerProbabilityDensityFunction(double x)
    {
        return Samples.Select(t => (x - t) / Smoothing).Select(i => Math.Exp(-i * i * 0.5)).Sum() * DistributionConstant;
    }

    public override string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(formatProvider, "Fn(x; S)");
    }
}