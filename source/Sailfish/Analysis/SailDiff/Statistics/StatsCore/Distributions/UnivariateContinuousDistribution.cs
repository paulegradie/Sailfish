using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;
using System;
using System.ComponentModel.DataAnnotations;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public abstract class UnivariateContinuousDistribution :
    DistributionBase,
    IUnivariateDistribution,
    IUnivariateDistribution<double>,
    ISampleableDistribution<double>
{
    private double? median;
    private double? mode;
    private DoubleRange? quartiles;
    private double? stdDev;

    public virtual double StandardDeviation
    {
        get
        {
            stdDev ??= Math.Sqrt(Variance);
            return stdDev.Value;
        }
    }

    public double[] Generate(int samples)
    {
        return Generate(samples, new double[samples], Generator.Random);
    }

    public double[] Generate(int samples, double[] result)
    {
        return Generate(samples, result, Generator.Random);
    }

    public double Generate()
    {
        return Generate(Generator.Random);
    }

    public double[] Generate(int samples, Random source)
    {
        return Generate(samples, new double[samples], source);
    }

    public virtual double[] Generate(int samples, double[] result, Random source)
    {
        for (var index = 0; index < samples; ++index)
            result[index] = InverseDistributionFunction(source.NextDouble());
        return result;
    }

    public virtual double Generate(Random source)
    {
        return InverseDistributionFunction(source.NextDouble());
    }

    double ISampleableDistribution<double>.Generate(double result)
    {
        return Generate();
    }

    double ISampleableDistribution<double>.Generate(double result, Random source)
    {
        return Generate(source);
    }

    public abstract double Mean { get; }

    public abstract double Variance { get; }

    public abstract double Entropy { get; }

    public abstract DoubleRange Support { get; }

    public virtual double Mode
    {
        get
        {
            if (!mode.HasValue)
            {
                Func<double, double> function = ProbabilityDensityFunction;
                var quartiles = Quartiles;
                var min = quartiles.Min;
                quartiles = Quartiles;
                var max = quartiles.Max;
                mode = BrentSearch.Maximize(function, min, max, 1E-10);
            }

            return mode.Value;
        }
    }

    public virtual DoubleRange Quartiles
    {
        get
        {
            if (!quartiles.HasValue)
                quartiles = new DoubleRange(InverseDistributionFunction(0.25), InverseDistributionFunction(0.75));
            return quartiles.Value;
        }
    }

    public virtual DoubleRange GetRange(double percentile)
    {
        var num1 = percentile > 0.0 && percentile <= 1.0
            ? InverseDistributionFunction(1.0 - percentile)
            : throw new ArgumentOutOfRangeException(nameof(percentile), "The percentile must be between 0 and 1.");
        var num2 = InverseDistributionFunction(percentile);
        return num2 > num1 ? new DoubleRange(num1, num2) : new DoubleRange(num2, num1);
    }

    public virtual double Median
    {
        get
        {
            if (!median.HasValue)
                median = InverseDistributionFunction(0.5);
            return median.Value;
        }
    }

    double IDistribution.DistributionFunction(double[] x)
    {
        return DistributionFunction(x[0]);
    }

    double IDistribution.ComplementaryDistributionFunction(double[] x)
    {
        return ComplementaryDistributionFunction(x[0]);
    }

    double IDistribution.ProbabilityFunction(double[] x)
    {
        return ProbabilityDensityFunction(x[0]);
    }

    double IUnivariateDistribution.ProbabilityFunction(double x)
    {
        return ProbabilityDensityFunction(x);
    }

    double IDistribution.LogProbabilityFunction(double[] x)
    {
        return LogProbabilityDensityFunction(x[0]);
    }

    double IUnivariateDistribution.LogProbabilityFunction(double x)
    {
        return LogProbabilityDensityFunction(x);
    }

    void IDistribution.Fit(Array observations)
    {
        ((IDistribution)this).Fit(observations, (IFittingOptions)null);
    }

    void IDistribution.Fit(Array observations, double[] weights)
    {
        ((IDistribution)this).Fit(observations, weights, null);
    }

    void IDistribution.Fit(Array observations, int[] weights)
    {
        ((IDistribution)this).Fit(observations, weights, null);
    }

    void IDistribution.Fit(Array observations, IFittingOptions options)
    {
        ((IDistribution)this).Fit(observations, (double[])null, options);
    }

    void IDistribution.Fit(Array observations, double[] weights, IFittingOptions options)
    {
        switch (observations)
        {
            case double[] observations1:
                Fit(observations1, weights, options);
                break;

            case double[][] vectors:
                Fit(vectors.Concatenate(), weights, options);
                break;

            default:
                throw new ArgumentException("Invalid input type.", nameof(observations));
        }
    }

    void IDistribution.Fit(Array observations, int[] weights, IFittingOptions options)
    {
        switch (observations)
        {
            case double[] observations1:
                Fit(observations1, weights, options);
                break;

            case double[][] vectors:
                Fit(vectors.Concatenate(), weights, options);
                break;

            default:
                throw new ArgumentException("Invalid input type.", nameof(observations));
        }
    }

    public virtual double DistributionFunction(double x)
    {
        if (double.IsNaN(x))
            throw new ArgumentOutOfRangeException(nameof(x), "The input argument is NaN.");
        if ((double.IsNegativeInfinity(Support.Min) && double.IsNegativeInfinity(x)) || x < Support.Min)
            return 0.0;
        if (x >= Support.Max)
            return 1.0;
        var d = InnerDistributionFunction(x);
        if (double.IsNaN(d))
            throw new InvalidOperationException("CDF computation generated NaN values.");
        if (d < 0.0 || d > 1.0) throw new InvalidOperationException("CDF computation generated values out of the [0,1] range.");
        return d;
    }

    public virtual double DistributionFunction(double a, double b)
    {
        if (a > b)
            throw new ArgumentOutOfRangeException(nameof(b), "The start of the interval a must be smaller than b.");
        return a == b ? 0.0 : DistributionFunction(b) - DistributionFunction(a);
    }

    public virtual double ComplementaryDistributionFunction(double x)
    {
        if (double.IsNaN(x))
            throw new ArgumentOutOfRangeException(nameof(x), "The input argument is NaN.");
        if ((double.IsNegativeInfinity(Support.Min) && double.IsNegativeInfinity(x)) || x < Support.Min)
            return 1.0;
        if (x >= Support.Max)
            return 0.0;
        var d = InnerComplementaryDistributionFunction(x);
        if (double.IsNaN(d))
            throw new InvalidOperationException("CCDF computation generated NaN values.");
        if (d < 0.0 || d > 1.0) throw new InvalidOperationException("CCDF computation generated values out of the [0,1] range.");

        return d;
    }

    public double InverseDistributionFunction([Range(0, 1)] double p)
    {
        if (p < 0.0 || p > 1.0)
            throw new ArgumentOutOfRangeException(nameof(p), "Value must be between 0 and 1.");
        if (double.IsNaN(p))
            throw new ArgumentOutOfRangeException(nameof(p), "Value is Not-a-Number (NaN).");
        if (p == 0.0)
            return Support.Min;
        if (p == 1.0)
            return Support.Max;
        var d = InnerInverseDistributionFunction(p);
        if (double.IsNaN(d))
            throw new InvalidOperationException("invCDF computation generated NaN values.");
        if (d < Support.Min || d > Support.Max) throw new InvalidOperationException("invCDF computation generated values outside the distribution supported range.");

        return d;
    }

    public virtual double QuantileDensityFunction(double p)
    {
        return 1.0 / ProbabilityDensityFunction(InverseDistributionFunction(p));
    }

    public virtual double HazardFunction(double x)
    {
        var num1 = ProbabilityDensityFunction(x);
        if (num1 == 0.0)
            return 0.0;
        var num2 = ComplementaryDistributionFunction(x);
        return num1 / num2;
    }

    public virtual double CumulativeHazardFunction(double x)
    {
        return -Math.Log(ComplementaryDistributionFunction(x));
    }

    public virtual double LogCumulativeHazardFunction(double x)
    {
        return Math.Log(-Math.Log(ComplementaryDistributionFunction(x)));
    }

    double IDistribution<double>.ProbabilityFunction(double x)
    {
        return ProbabilityDensityFunction(x);
    }

    double IDistribution<double>.LogProbabilityFunction(double x)
    {
        return LogProbabilityDensityFunction(x);
    }

    protected internal virtual double InnerDistributionFunction(double x)
    {
        throw new NotImplementedException();
    }

    protected internal virtual double InnerComplementaryDistributionFunction(double x)
    {
        return 1.0 - DistributionFunction(x);
    }

    protected internal virtual double InnerInverseDistributionFunction(double p)
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
                if (num3 >= p)
                    return num1;
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
                if (num4 >= p)
                    return num2;
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
                if (num5 >= p)
                    return 0.0;
                for (; num5 < p && !double.IsInfinity(num2); num5 = DistributionFunction(num2))
                {
                    num1 = num2;
                    num2 = 2.0 * num2 + 1.0;
                }
            }
        }

        if (double.IsNegativeInfinity(num1))
            num1 = double.MinValue;
        if (double.IsPositiveInfinity(num2))
            num2 = double.MaxValue;
        return BrentSearch.Find(DistributionFunction, p, num1, num2);
    }

    public virtual double ProbabilityDensityFunction(double x)
    {
        if (double.IsNaN(x))
            throw new ArgumentOutOfRangeException(nameof(x), "The input argument is NaN.");
        if (x < Support.Min || x > Support.Max)
            return 0.0;
        var d = InnerProbabilityDensityFunction(x);
        return !double.IsNaN(d) ? d : throw new InvalidOperationException("PDF computation generated NaN values.");
    }

    protected internal virtual double InnerProbabilityDensityFunction(double x)
    {
        throw new NotImplementedException();
    }

    public virtual double LogProbabilityDensityFunction(double x)
    {
        if (double.IsNaN(x))
            throw new ArgumentOutOfRangeException(nameof(x), "The input argument is NaN.");
        if (x < Support.Min || x > Support.Max)
            return double.NegativeInfinity;
        var d = InnerLogProbabilityDensityFunction(x);
        return !double.IsNaN(d) ? d : throw new InvalidOperationException("LogPDF computation generated NaN values.");
    }

    protected internal virtual double InnerLogProbabilityDensityFunction(double x)
    {
        return Math.Log(ProbabilityDensityFunction(x));
    }

    public void Fit(double[] observations, double[] weights)
    {
        Fit(observations, weights, null);
    }

    public virtual void Fit(double[] observations, double[] weights, IFittingOptions options)
    {
        throw new NotSupportedException();
    }

    public virtual void Fit(double[] observations, int[] weights, IFittingOptions options)
    {
        if (weights != null)
            throw new NotSupportedException();
        Fit(observations, (double[])null, options);
    }
}