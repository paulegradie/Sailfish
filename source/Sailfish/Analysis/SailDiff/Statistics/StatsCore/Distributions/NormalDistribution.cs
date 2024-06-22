using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

internal sealed class NormalDistribution : UnivariateContinuousDistribution, IFormattable
{
    private readonly double lnconstant;
    private readonly double stdDev = 1.0;
    private readonly double variance = 1.0;

    public NormalDistribution(double mean, double stdDev)
    {
        this.Mean = mean;
        this.stdDev = stdDev;
        variance = stdDev * stdDev;
        lnconstant = -Math.Log(2.5066282746310007 * stdDev);
    }

    public override double Mean { get; }

    public override DoubleRange Support => new(double.NegativeInfinity, double.PositiveInfinity);

    public override string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(formatProvider, "N(x; μ = {0}, σ\u00B2 = {1})", Mean.ToString(format, formatProvider), variance.ToString(format, formatProvider));
    }

    protected override double InnerDistributionFunction(double x)
    {
        return Normal.Function((x - Mean) / stdDev);
    }

    protected override double InnerComplementaryDistributionFunction(double x)
    {
        return Normal.Complemented((x - Mean) / stdDev);
    }

    protected override double InnerInverseDistributionFunction(double p)
    {
        return Mean + stdDev * Normal.Inverse(p);
    }

    protected override double InnerProbabilityDensityFunction(double x)
    {
        var zScore = (x - Mean) / stdDev;
        return Math.Exp(lnconstant - zScore * zScore * 0.5);
    }

    protected override double InnerLogProbabilityDensityFunction(double x)
    {
        var zScore = (x - Mean) / stdDev;
        return lnconstant - zScore * zScore * 0.5;
    }
}