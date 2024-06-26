using System;
using System.ComponentModel.DataAnnotations;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;

internal abstract class UnivariateContinuousDistribution : DistributionBase
{
    public abstract double Mean { get; }

    public abstract DoubleRange Support { get; }

    public double[] Generate(int samples, Random source)
    {
        var result = new double[samples];
        for (var index = 0; index < samples; ++index) result[index] = InverseDistributionFunction(source.NextDouble());

        return result;
    }

    public virtual double DistributionFunction(double x)
    {
        if (double.IsNaN(x)) throw new ArgumentOutOfRangeException(nameof(x), "The input argument is NaN.");

        if ((double.IsNegativeInfinity(Support.Min) && double.IsNegativeInfinity(x)) || x < Support.Min) return 0.0;

        if (x >= Support.Max) return 1.0;

        var d = InnerDistributionFunction(x);
        return d switch
        {
            double.NaN => throw new InvalidOperationException("CDF computation generated NaN values."),
            < 0.0 or > 1.0 => throw new InvalidOperationException("CDF computation generated values out of the [0,1] range."),
            _ => d
        };
    }

    public double ComplementaryDistributionFunction(double x)
    {
        if (double.IsNaN(x)) throw new ArgumentOutOfRangeException(nameof(x), "The input argument is NaN.");

        if ((double.IsNegativeInfinity(Support.Min) && double.IsNegativeInfinity(x)) || x < Support.Min) return 1.0;

        if (x >= Support.Max) return 0.0;

        var d = InnerComplementaryDistributionFunction(x);
        return d switch
        {
            double.NaN => throw new InvalidOperationException("CCDF computation generated NaN values."),
            < 0.0 or > 1.0 => throw new InvalidOperationException("CCDF computation generated values out of the [0,1] range."),
            _ => d
        };
    }

    public double InverseDistributionFunction([Range(0, 1)] double p)
    {
        switch (p)
        {
            case < 0.0 or > 1.0:
                throw new ArgumentOutOfRangeException(nameof(p), "Value must be between 0 and 1.");
            case double.NaN:
                throw new ArgumentOutOfRangeException(nameof(p), "Value is Not-a-Number (NaN).");
            case 0.0:
                return Support.Min;
            case 1.0:
                return Support.Max;
        }

        var d = InnerInverseDistributionFunction(p);
        if (double.IsNaN(d)) throw new InvalidOperationException("invCDF computation generated NaN values.");

        if (d < Support.Min || d > Support.Max) throw new InvalidOperationException("invCDF computation generated values outside the distribution supported range.");

        return d;
    }

    protected virtual double InnerDistributionFunction(double x)
    {
        throw new NotImplementedException();
    }

    protected virtual double InnerComplementaryDistributionFunction(double x)
    {
        return 1.0 - DistributionFunction(x);
    }

    protected virtual double InnerInverseDistributionFunction(double p)
    {
        var flag1 = !double.IsInfinity(Support.Min);
        var flag2 = !double.IsInfinity(Support.Max);
        double num1;
        double num2;
        if (flag1 & flag2)
        {
            var support = Support;
            num1 = support.Min;
            support = Support;
            num2 = support.Max;
        }
        else if (flag1 && !flag2)
        {
            num1 = Support.Min;
            num2 = num1 + 1.0;
            var num3 = DistributionFunction(num1);
            if (num3 > p)
            {
                for (; num3 > p && !double.IsInfinity(num2); num3 = DistributionFunction(num2))
                    num2 += 2.0 * (num2 - num1) + 1.0;
            }
            else
            {
                if (num3 >= p) return num1;

                for (; num3 < p && !double.IsInfinity(num2); num3 = DistributionFunction(num2))
                    num2 += 2.0 * (num2 - num1) + 1.0;
            }
        }
        else if (!flag1 & flag2)
        {
            num2 = Support.Max;
            num1 = num2 - 1.0;
            var num4 = DistributionFunction(num2);
            if (num4 > p)
            {
                for (; num4 > p && !double.IsInfinity(num1); num4 = DistributionFunction(num1))
                    num1 -= 2.0 * num1;
            }
            else
            {
                if (num4 >= p) return num2;

                for (; num4 < p && !double.IsInfinity(num1); num4 = DistributionFunction(num1))
                    num1 -= 2.0 * num1;
            }
        }
        else
        {
            num1 = 0.0;
            num2 = 0.0;
            var num5 = DistributionFunction(0.0);
            if (num5 > p)
            {
                for (; num5 > p && !double.IsInfinity(num1); num5 = DistributionFunction(num1))
                {
                    num2 = num1;
                    num1 = 2.0 * num1 - 1.0;
                }
            }
            else
            {
                if (num5 >= p) return 0.0;

                for (; num5 < p && !double.IsInfinity(num2); num5 = DistributionFunction(num2))
                {
                    num1 = num2;
                    num2 = 2.0 * num2 + 1.0;
                }
            }
        }

        if (double.IsNegativeInfinity(num1)) num1 = double.MinValue;

        if (double.IsPositiveInfinity(num2)) num2 = double.MaxValue;

        return BrentSearch.Find(DistributionFunction, p, num1, num2);
    }

    public double ProbabilityDensityFunction(double x)
    {
        if (double.IsNaN(x)) throw new ArgumentOutOfRangeException(nameof(x), "The input argument is NaN.");

        if (x < Support.Min || x > Support.Max) return 0.0;

        var d = InnerProbabilityDensityFunction(x);
        return !double.IsNaN(d) ? d : throw new InvalidOperationException("PDF computation generated NaN values.");
    }

    protected virtual double InnerProbabilityDensityFunction(double x)
    {
        throw new NotImplementedException();
    }

    public double LogProbabilityDensityFunction(double x)
    {
        if (double.IsNaN(x)) throw new ArgumentOutOfRangeException(nameof(x), "The input argument is NaN.");

        if (x < Support.Min || x > Support.Max) return double.NegativeInfinity;

        var d = InnerLogProbabilityDensityFunction(x);
        return !double.IsNaN(d) ? d : throw new InvalidOperationException("LogPDF computation generated NaN values.");
    }

    protected virtual double InnerLogProbabilityDensityFunction(double x)
    {
        return Math.Log(ProbabilityDensityFunction(x));
    }
}