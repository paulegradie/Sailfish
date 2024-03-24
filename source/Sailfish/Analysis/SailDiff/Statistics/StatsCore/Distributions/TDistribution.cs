using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class TDistribution : UnivariateContinuousDistribution, IFormattable
{
    private readonly double lnconstant;

    public TDistribution([Positive(1.0)] double degreesOfFreedom)
    {
        DegreesOfFreedom = degreesOfFreedom >= 1.0 ? degreesOfFreedom : throw new ArgumentOutOfRangeException(nameof(degreesOfFreedom));
        var num = degreesOfFreedom;
        lnconstant = Gamma.Log((num + 1.0) / 2.0) - (0.5 * Math.Log(num * Math.PI) + Gamma.Log(num / 2.0));
    }

    public double DegreesOfFreedom { get; }

    public override double Mean => DegreesOfFreedom <= 1.0 ? double.NaN : 0.0;

    public override double Mode => 0.0;

    public override double Variance
    {
        get
        {
            if (DegreesOfFreedom > 2.0)
                return DegreesOfFreedom / (DegreesOfFreedom - 2.0);
            return DegreesOfFreedom > 1.0 ? double.PositiveInfinity : double.NaN;
        }
    }

    public override DoubleRange Support => new(double.NegativeInfinity, double.PositiveInfinity);

    public override double Entropy => throw new NotSupportedException();

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        return $"T(x; df = {(object)DegreesOfFreedom.ToString(format, formatProvider)})";
    }

    protected internal override double InnerDistributionFunction(double x)
    {
        var degreesOfFreedom = DegreesOfFreedom;
        var num = Math.Sqrt(x * x + degreesOfFreedom);
        var x1 = (x + num) / (2.0 * num);
        return Beta.Incomplete(degreesOfFreedom / 2.0, degreesOfFreedom / 2.0, x1);
    }

    protected internal override double InnerProbabilityDensityFunction(double x)
    {
        return Math.Exp(LogProbabilityDensityFunction(x));
    }

    protected internal override double InnerInverseDistributionFunction(double p)
    {
        return inverseDistributionLeftTail(DegreesOfFreedom, p);
    }


    public override object Clone()
    {
        return new TDistribution(DegreesOfFreedom);
    }

    private static double inverseDistributionLeftTail(double df, double p)
    {
        if (p == 0.0)
            return double.NegativeInfinity;
        if (p == 1.0)
            return double.PositiveInfinity;
        if (p > 0.25 && p < 0.75)
        {
            if (p == 0.5)
                return 0.0;
            var num1 = 1.0 - 2.0 * p;
            var num2 = Beta.IncompleteInverse(0.5, 0.5 * df, Math.Abs(num1));
            var num3 = Math.Sqrt(df * num2 / (1.0 - num2));
            if (p < 0.5)
                num3 = -num3;
            return num3;
        }

        var num4 = -1;
        if (p >= 0.5)
        {
            p = 1.0 - p;
            num4 = 1;
        }

        var num5 = Beta.IncompleteInverse(0.5 * df, 0.5, 2.0 * p);
        if (double.MaxValue * num5 < df)
            return num4 * double.MaxValue;
        var num6 = Math.Sqrt(df / num5 - df);
        return num4 * num6;
    }
}