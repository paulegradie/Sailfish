using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class NonCentralTDistribution([Positive] double degreesOfFreedom, [Real] double nonCentrality) : UnivariateContinuousDistribution, IFormattable
{
    private double? mode;

    public double DegreesOfFreedom { get; } = degreesOfFreedom > 0.0
            ? degreesOfFreedom
            : throw new ArgumentOutOfRangeException(nameof(degreesOfFreedom), "The number of degrees of freedom must be positive.");

    public double NonCentrality { get; } = nonCentrality;

    public override double Mean => DegreesOfFreedom > 1.0
        ? NonCentrality * Math.Sqrt(DegreesOfFreedom / 2.0) * Gamma.Function((DegreesOfFreedom - 1.0) / 2.0) / Gamma.Function(DegreesOfFreedom / 2.0)
        : double.NaN;

    public override double Variance
    {
        get
        {
            if (DegreesOfFreedom <= 2.0)
                return double.NaN;
            var num1 = DegreesOfFreedom * (1.0 + NonCentrality * NonCentrality) / (DegreesOfFreedom - 2.0);
            var num2 = NonCentrality * NonCentrality * DegreesOfFreedom / 2.0;
            var num3 = Gamma.Function((DegreesOfFreedom - 1.0) / 2.0) / Gamma.Function(DegreesOfFreedom / 2.0);
            var num4 = num2 * num3 * num3;
            return num1 - num4;
        }
    }

    public override double Mode
    {
        get
        {
            if (mode.HasValue) return mode.Value;
            if (NonCentrality == 0.0)
            {
                var num = Gamma.Function((DegreesOfFreedom + 2.0) / 2.0) / Gamma.Function((DegreesOfFreedom + 3.0) / 3.0);
                mode = Math.Sqrt(2.0 / DegreesOfFreedom) * num * NonCentrality;
            }
            else if (double.IsInfinity(NonCentrality))
            {
                mode = Math.Sqrt(DegreesOfFreedom / (DegreesOfFreedom + 1.0)) * NonCentrality;
            }
            else
            {
                var num1 = Math.Sqrt(2.0 * DegreesOfFreedom / (2.0 * DegreesOfFreedom + 5.0)) * NonCentrality;
                var num2 = Math.Sqrt(DegreesOfFreedom / (DegreesOfFreedom + 1.0)) * NonCentrality;
                mode = num1 <= num2 ? BrentSearch.Maximize(ProbabilityDensityFunction, num1, num2) : BrentSearch.Maximize(ProbabilityDensityFunction, num2, num1);
            }

            return mode.Value;
        }
    }

    public override double Entropy => throw new NotSupportedException();

    public override DoubleRange Support => new(double.NegativeInfinity, double.PositiveInfinity);

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        var provider = formatProvider;
        var num = DegreesOfFreedom;
        var str1 = num.ToString(format, formatProvider);
        num = NonCentrality;
        var str2 = num.ToString(format, formatProvider);
        return string.Format(provider, "T(x; df = {0}, μ = {1})", str1, str2);
    }

    protected internal override double InnerDistributionFunction(double x)
    {
        return DistributionFunctionLowerTail(x, DegreesOfFreedom, NonCentrality);
    }

    protected internal override double InnerProbabilityDensityFunction(double x)
    {
        var noncentrality = NonCentrality;
        var degreesOfFreedom = DegreesOfFreedom;
        var func = DistributionFunctionLowerTail;
        if (x != 0.0)
        {
            var num1 = func(x * Math.Sqrt(1.0 + 2.0 / degreesOfFreedom), degreesOfFreedom + 2.0, noncentrality);
            var num2 = func(x, degreesOfFreedom, noncentrality);
            return degreesOfFreedom / x * (num1 - num2);
        }

        var num3 = Gamma.Function((degreesOfFreedom + 1.0) / 2.0);
        var num4 = Math.Sqrt(Math.PI * degreesOfFreedom) * Gamma.Function(degreesOfFreedom / 2.0);
        var num5 = Math.Exp(-(noncentrality * noncentrality) / 2.0);
        return num3 / num4 * num5;
    }

    public override object Clone()
    {
        return new NonCentralTDistribution(DegreesOfFreedom, NonCentrality);
    }

    private static double DistributionFunctionLowerTail(double t, double df, double delta)
    {
        const double num1 = 0.5723649429247001;
        const double num2 = 1E-10;
        const int num3 = 100;
        const double num4 = 0.7978845608028654;
        if (df <= 0.0)
            throw new ArgumentOutOfRangeException(nameof(df), "Degrees of freedom must be positive.");
        double num5;
        bool flag;
        if (t < 0.0)
        {
            num5 = -delta;
            flag = true;
        }
        else
        {
            num5 = delta;
            flag = false;
        }

        var num6 = 1.0;
        var num7 = t * t / (t * t + df);
        if (double.IsNaN(num7))
            num7 = 1.0;
        const double num8 = 0.0;
        if (num7 <= 0.0)
        {
            var num9 = num8 + Normal.Complemented(num5);
            if (flag)
                num9 = 1.0 - num9;
            return num9;
        }

        var num10 = num5 * num5;
        var num11 = 0.5 * Math.Exp(-0.5 * num10);
        var num12 = num4 * num11 * num5;
        var num13 = 0.5 - num11;
        var a = 0.5;
        var num14 = 0.5 * df;
        var num15 = Math.Pow(1.0 - num7, num14);
        var num16 = num1 + Gamma.Log(num14) - Gamma.Log(a + num14);
        var num17 = Beta.Incomplete(a, num14, num7);
        var num18 = 2.0 * num15 * Math.Exp(a * Math.Log(num7) - num16);
        var num19 = 1.0 - num15;
        var num20 = num14 * num7 * num15;
        var num21 = num11 * num17 + num12 * num19;
        do
        {
            ++a;
            num17 -= num18;
            num19 -= num20;
            num18 = num18 * num7 * (a + num14 - 1.0) / a;
            num20 = num20 * num7 * (a + num14 - 0.5) / (a + 0.5);
            num11 = num11 * num10 / (2.0 * num6);
            num12 = num12 * num10 / (2.0 * num6 + 1.0);
            num13 -= num11;
            ++num6;
            num21 = num21 + num11 * num17 + num12 * num19;
            var d = 2.0 * num13 * (num17 - num18);
            if (d is <= num2 or double.NaN)
                goto label_15;
        } while (num3 >= num6);

        throw new ConvergenceException("Maximum number of iterations reached.");
    label_15:
        var num22 = num21 + Normal.Complemented(num5);
        if (flag)
            num22 = 1.0 - num22;
        return num22;
    }
}