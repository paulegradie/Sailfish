using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

internal sealed class NormalDistribution : UnivariateContinuousDistribution, IFormattable
{
    private readonly double _lnconstant;
    private readonly double _stdDev = 1.0;
    private readonly double _variance = 1.0;

    public NormalDistribution(double mean, double stdDev)
    {
        Mean = mean;
        _stdDev = stdDev;
        _variance = stdDev * stdDev;
        _lnconstant = -Math.Log(2.5066282746310007 * stdDev);
    }

    public override double Mean { get; }

    public override DoubleRange Support => new(double.NegativeInfinity, double.PositiveInfinity);

    public override string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(formatProvider, "N(x; μ = {0}, σ\u00B2 = {1})", Mean.ToString(format, formatProvider), _variance.ToString(format, formatProvider));
    }

    protected override double InnerDistributionFunction(double x)
    {
        return Normal.Function((x - Mean) / _stdDev);
    }

    protected override double InnerComplementaryDistributionFunction(double x)
    {
        return Normal.Complemented((x - Mean) / _stdDev);
    }

    protected override double InnerInverseDistributionFunction(double p)
    {
        return Mean + _stdDev * Normal.Inverse(p);
    }

    protected override double InnerProbabilityDensityFunction(double x)
    {
        var zScore = (x - Mean) / _stdDev;
        return Math.Exp(_lnconstant - zScore * zScore * 0.5);
    }

    protected override double InnerLogProbabilityDensityFunction(double x)
    {
        var zScore = (x - Mean) / _stdDev;
        return _lnconstant - zScore * zScore * 0.5;
    }
}