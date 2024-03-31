using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class Distribution : UnivariateContinuousDistribution, IFormattable
{
    public Distribution([Positive(1.0)] double degreesOfFreedom)
    {
        DegreesOfFreedom = degreesOfFreedom >= 1.0 ? degreesOfFreedom : throw new ArgumentOutOfRangeException(nameof(degreesOfFreedom));
    }

    public double DegreesOfFreedom { get; }

    public override double Mean => DegreesOfFreedom <= 1.0 ? double.NaN : 0.0;

    public override DoubleRange Support => new(double.NegativeInfinity, double.PositiveInfinity);

    public override string ToString(string? format, IFormatProvider? formatProvider)
    {
        return $"T(x; df = {(object)DegreesOfFreedom.ToString(format, formatProvider)})";
    }

    protected internal override double InnerDistributionFunction(double x)
    {
        var num = Math.Sqrt(x * x + DegreesOfFreedom);
        var x1 = (x + num) / (2.0 * num);
        return Beta.Incomplete(DegreesOfFreedom / 2.0, DegreesOfFreedom / 2.0, x1);
    }

    protected internal override double InnerProbabilityDensityFunction(double x)
    {
        return Math.Exp(LogProbabilityDensityFunction(x));
    }

    protected internal override double InnerInverseDistributionFunction(double p)
    {
        return InverseDistributionLeftTail(DegreesOfFreedom, p);
    }


    public override object Clone()
    {
        return new Distribution(DegreesOfFreedom);
    }

    private static double InverseDistributionLeftTail(double df, double p)
    {
        if (p == 0.0)
            return double.NegativeInfinity;
        if (p == 1.0)
            return double.PositiveInfinity;
        if (p is > 0.25 and < 0.75)
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