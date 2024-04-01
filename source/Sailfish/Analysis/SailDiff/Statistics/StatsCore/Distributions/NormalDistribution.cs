using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

internal sealed class NormalDistribution : UnivariateContinuousDistribution, IFormattable
{
    private readonly double lnconstant;
    private readonly double mean;
    private readonly double stdDev = 1.0;
    private readonly double variance = 1.0;

    public NormalDistribution(double mean, double stdDev)
    {
        this.mean = mean;
        this.stdDev = stdDev;
        variance = stdDev * stdDev;
        lnconstant = -Math.Log(2.5066282746310007 * stdDev);
    }

    public override string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(formatProvider, "N(x; μ = {0}, σ\u00B2 = {1})", mean.ToString(format, formatProvider), variance.ToString(format, formatProvider));
    }

    public override double Mean => mean;

    public override DoubleRange Support => new(double.NegativeInfinity, double.PositiveInfinity);

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
        var zScore = (x - mean) / stdDev;
        return Math.Exp(lnconstant - zScore * zScore * 0.5);
    }

    protected internal override double InnerLogProbabilityDensityFunction(double x)
    {
        var zScore = (x - mean) / stdDev;
        return lnconstant - zScore * zScore * 0.5;
    }
}