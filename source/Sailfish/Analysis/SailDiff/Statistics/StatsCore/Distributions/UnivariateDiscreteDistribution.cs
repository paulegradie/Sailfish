using System;
using System.ComponentModel.DataAnnotations;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Serializable]
public abstract class UnivariateDiscreteDistribution : DistributionBase,
    IUnivariateDistribution<int>,
    IUnivariateDistribution,
    IUnivariateDistribution<double>,
    IDistribution<double[]>,
    ISampleableDistribution<double>,
    ISampleableDistribution<int>,
    IRandomNumberGenerator<double>
{
    private double? median;
    private DoubleRange? quartiles;

    public abstract double Mean { get; }

    public abstract double Variance { get; }

    public abstract double Entropy { get; }

    public abstract IntRange Support { get; }

    public virtual double Mode => Mean;

    public virtual double Median
    {
        get
        {
            median ??= InverseDistributionFunction(0.5);

#if DEBUG
            double expected = BaseInverseDistributionFunction(0.5);
            if (median != expected)
                throw new Exception();
#endif

            return median.Value;
        }
    }

    public virtual DoubleRange Quartiles
    {
        get
        {
            if (quartiles == null)
            {
                double min = InverseDistributionFunction(0.25);
                double max = InverseDistributionFunction(0.75);
                quartiles = new DoubleRange(min, max);
            }

            return quartiles.Value;
        }
    }

    public virtual IntRange GetRange(double percentile)
    {
        if (percentile is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(percentile), "The percentile must be between 0 and 1.");

        var a = InverseDistributionFunction(1.0 - percentile);
        var b = InverseDistributionFunction(percentile);

        if (b > a)
            return new IntRange(a, b);
        return new IntRange(b, a);
    }

    #region IDistribution explicit members

    DoubleRange IUnivariateDistribution.Support => new(Support.Min, Support.Max);

    DoubleRange IUnivariateDistribution<int>.Support => new(Support.Min, Support.Max);

    DoubleRange IUnivariateDistribution<double>.Support => new(Support.Min, Support.Max);

    DoubleRange IUnivariateDistribution.GetRange(double percentile)
    {
        var range = GetRange(percentile);
        return new DoubleRange(range.Min, range.Max);
    }

    double IDistribution.DistributionFunction(double[] x)
    {
        if (double.IsNegativeInfinity(x[0]))
            return DistributionFunction(int.MinValue);
        if (double.IsPositiveInfinity(x[0]))
            return DistributionFunction(int.MaxValue);

        return DistributionFunction((int)x[0]);
    }

    double IUnivariateDistribution.DistributionFunction(double x)
    {
        if (double.IsNegativeInfinity(x))
            return DistributionFunction(int.MinValue);
        if (double.IsPositiveInfinity(x))
            return DistributionFunction(int.MaxValue);

        return DistributionFunction((int)x);
    }

    double IUnivariateDistribution.DistributionFunction(double a, double b)
    {
        int ia;
        if (double.IsNegativeInfinity(a))
            ia = int.MinValue;
        else if (double.IsPositiveInfinity(a))
            ia = int.MaxValue;
        else ia = (int)a;

        int ib;
        if (double.IsNegativeInfinity(b))
            ib = int.MinValue;
        else if (double.IsPositiveInfinity(b))
            ib = int.MaxValue;
        else ib = (int)b;

        return DistributionFunction(ia, ib);
    }

    double IUnivariateDistribution.InverseDistributionFunction(double p)
    {
        return InverseDistributionFunction(p);
    }

    double IDistribution.ComplementaryDistributionFunction(double[] x)
    {
        if (double.IsNegativeInfinity(x[0]))
            return ComplementaryDistributionFunction(int.MinValue);
        if (double.IsPositiveInfinity(x[0]))
            return ComplementaryDistributionFunction(int.MaxValue);

        return ComplementaryDistributionFunction((int)x[0]);
    }

    double IDistribution.ProbabilityFunction(double[] x)
    {
        if (double.IsNegativeInfinity(x[0]))
            return ProbabilityMassFunction(int.MinValue);
        if (double.IsPositiveInfinity(x[0]))
            return ProbabilityMassFunction(int.MaxValue);

        return ProbabilityMassFunction((int)x[0]);
    }

    double IDistribution.LogProbabilityFunction(double[] x)
    {
        return LogProbabilityMassFunction((int)x[0]);
    }

    double IUnivariateDistribution.ProbabilityFunction(double x)
    {
        return ProbabilityMassFunction((int)x);
    }

    double IUnivariateDistribution.LogProbabilityFunction(double x)
    {
        return LogProbabilityMassFunction((int)x);
    }

    double IUnivariateDistribution.ComplementaryDistributionFunction(double x)
    {
        return ComplementaryDistributionFunction((int)x);
    }

    double IUnivariateDistribution.HazardFunction(double x)
    {
        return HazardFunction((int)x);
    }

    double IUnivariateDistribution.CumulativeHazardFunction(double x)
    {
        return CumulativeHazardFunction((int)x);
    }

    double IUnivariateDistribution.LogCumulativeHazardFunction(double x)
    {
        return LogCumulativeHazardFunction((int)x);
    }

    #endregion IDistribution explicit members

    public
#if COMPATIBILITY
        virtual
#endif
        double DistributionFunction(int k)
    {
        if (k < Support.Min)
            return 0;

        if (k >= Support.Max)
            return 1;

        var result = InnerDistributionFunction(k);

        if (double.IsNaN(result))
            throw new InvalidOperationException("CDF computation generated NaN values.");
        if (result is < 0 or > 1)
            new InvalidOperationException("CDF computation generated values out of the [0,1] range.");

        return result;
    }

#if COMPATIBILITY
        protected internal virtual double InnerDistributionFunction(int k)
        {
            throw new NotImplementedException();
        }
#else

    protected internal abstract double InnerDistributionFunction(int k);

#endif

    public
#if COMPATIBILITY
        virtual
#endif
        double DistributionFunction(int k, bool inclusive)
    {
        if (inclusive)
            return DistributionFunction(k);
        return DistributionFunction(k) - ProbabilityMassFunction(k);
    }

    public double DistributionFunction(int a, int b)
    {
        if (a >= b)
            throw new ArgumentOutOfRangeException(nameof(b),
                "The start of the interval a must be smaller than b.");

        return DistributionFunction(b, true) - DistributionFunction(a);
    }

    public int InverseDistributionFunction([Range(0, 1)] double p)
    {
        if (p is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(p), "Value must be between 0 and 1.");

        if (double.IsNaN(p))
            throw new ArgumentOutOfRangeException(nameof(p), "Value is Not-a-Number (NaN).");

        if (p == 0)
        {
            if (Support.Min == Support.Max)
                return Support.Min - 1;
            return Support.Min;
        }

        if (p == 1)
            return Support.Max;

        var result = InnerInverseDistributionFunction(p);

        if (result < Support.Min || result > Support.Max)
            throw new InvalidOperationException("invCDF computation generated values outside the distribution supported range.");

        return result;
    }

    protected virtual int InnerInverseDistributionFunction(double p)
    {
        return BaseInverseDistributionFunction(p);
    }

    protected int BaseInverseDistributionFunction(double p)
    {
        var lowerBounded = !double.IsInfinity(Support.Min);
        var upperBounded = !double.IsInfinity(Support.Max);

        if (lowerBounded && upperBounded) return new BinarySearch(DistributionFunction, Support.Min, Support.Max).Find(p);

        try
        {
            checked
            {
                if (lowerBounded && !upperBounded)
                {
                    var lower = Support.Min;
                    var upper = lower + 1;

                    var f = DistributionFunction(lower);

                    if (f > p)
                        while (f > p)
                        {
                            upper = 2 * upper;
                            f = DistributionFunction(upper);
                        }
                    else
                        while (f < p)
                        {
                            upper = 2 * upper;
                            f = DistributionFunction(upper);
                        }

                    return new BinarySearch(DistributionFunction, lower, upper).Find(p);
                }

                if (!lowerBounded && upperBounded)
                {
                    var upper = Support.Max;
                    var lower = upper - 1;

                    var f = DistributionFunction(upper);

                    if (f > p)
                        while (f > p)
                        {
                            lower = lower - 2 * lower;
                            f = DistributionFunction(lower);
                        }
                    else
                        while (f < p)
                        {
                            lower = lower - 2 * lower;
                            f = DistributionFunction(lower);
                        }

                    return new BinarySearch(DistributionFunction, lower, upper).Find(p);
                }

                return UnboundedBaseInverseDistributionFunction(p, -1, +1, 0);
            }
        }
        catch (OverflowException)
        {
            return 0;
        }
    }

    private int UnboundedBaseInverseDistributionFunction(double p, int lower, int upper, int start)
    {
        checked
        {
            var f = DistributionFunction(start);

            if (f > p)
                while (f > p)
                {
                    upper = lower;
                    lower = 2 * lower - 1;
                    f = DistributionFunction(lower);
                }
            else
                while (f < p)
                {
                    lower = upper;
                    upper = 2 * upper + 1;
                    f = DistributionFunction(upper);
                }

            return new BinarySearch(DistributionFunction, lower, upper).Find(p);
        }
    }

    public virtual double QuantileDensityFunction(double p)
    {
        return 1.0 / ProbabilityMassFunction(InverseDistributionFunction(p));
    }

    public
#if COMPATIBILITY
        virtual
#endif
        double ComplementaryDistributionFunction(int k)
    {
        if (k < Support.Min)
            return 1;
        if (k >= Support.Max)
            return 0;

        var result = InnerComplementaryDistributionFunction(k);

        if (double.IsNaN(result))
            throw new InvalidOperationException("CCDF computation generated NaN values.");
        if (result is < 0 or > 1)
            new InvalidOperationException("CCDF computation generated values out of the [0,1] range.");

        return result;
    }

    protected internal virtual double InnerComplementaryDistributionFunction(int k)
    {
        return 1.0 - DistributionFunction(k);
    }

    public
#if COMPATIBILITY
        virtual
#endif
        double ProbabilityMassFunction(int k)
    {
        if (k < Support.Min)
            return 0;
        if (k > Support.Max)
            return 0;

        var result = InnerProbabilityMassFunction(k);

        if (double.IsNaN(result))
            throw new InvalidOperationException("PMF computation generated NaN values.");

        return result;
    }

#if COMPATIBILITY
        protected internal virtual double InnerProbabilityMassFunction(int k)
        {
            throw new NotImplementedException();
        }
#else

    protected internal abstract double InnerProbabilityMassFunction(int k);

#endif

    public
#if COMPATIBILITY
        virtual
#endif
        double LogProbabilityMassFunction(int k)
    {
        if (k < Support.Min || k > Support.Max)
            return double.NegativeInfinity;

        var result = InnerLogProbabilityMassFunction(k);

        if (double.IsNaN(result))
            throw new InvalidOperationException("LogPDF computation generated NaN values.");

        return result;
    }

    protected internal virtual double InnerLogProbabilityMassFunction(int k)
    {
        return Math.Log(ProbabilityMassFunction(k));
    }

    public virtual double HazardFunction(int x)
    {
        return ProbabilityMassFunction(x) / ComplementaryDistributionFunction(x);
    }

    public virtual double CumulativeHazardFunction(int x)
    {
        return -Math.Log(ComplementaryDistributionFunction(x));
    }

    public virtual double LogCumulativeHazardFunction(int x)
    {
        return Math.Log(-Math.Log(ComplementaryDistributionFunction(x)));
    }

    public virtual void Fit(double[] observations)
    {
        Fit(observations, (IFittingOptions)null);
    }

    public virtual void Fit(double[] observations, double[]? weights)
    {
        Fit(observations, weights, null);
    }

    public virtual void Fit(double[] observations, int[] weights)
    {
        Fit(observations, weights, null);
    }

    public virtual void Fit(double[] observations, IFittingOptions? options)
    {
        Fit(observations, (double[])null, options);
    }

    public virtual void Fit(double[] observations, double[]? weights, IFittingOptions? options)
    {
        throw new NotSupportedException();
    }

    public virtual void Fit(double[] observations, int[]? weights, IFittingOptions? options)
    {
        if (weights == null)
            Fit(observations, (double[])null, options);
        else
            throw new NotSupportedException();
    }

    public virtual void Fit(int[] observations)
    {
        Fit(observations, null);
    }

    public virtual void Fit(int[] observations, IFittingOptions options)
    {
        Fit(observations, (double[])null, options);
    }

    public virtual void Fit(int[] observations, double[]? weights, IFittingOptions options)
    {
        throw new NotSupportedException();
    }

    public virtual void Fit(int[] observations, int[] weights, IFittingOptions options)
    {
        if (weights != null)
            throw new NotSupportedException();

        Fit(observations, (double[])null, options);
    }

    public int[] Generate(int samples)
    {
        return Generate(samples, Generator.Random);
    }

    public int[] Generate(int samples, int[] result)
    {
        return Generate(samples, result, Generator.Random);
    }

    double[] StatsCore.IRandomNumberGenerator<double>.Generate(int samples)
    {
        throw new NotImplementedException();
    }

    public double[] Generate(int samples, double[] result)
    {
        return Generate(samples, result, Generator.Random);
    }

    double StatsCore.IRandomNumberGenerator<double>.Generate()
    {
        return Generate();
    }

    public int Generate()
    {
        return Generate(Generator.Random);
    }

    public int[] Generate(int samples, Random source)
    {
        return Generate(samples, new int[samples]);
    }

    public virtual int[] Generate(int samples, int[] result, Random source)
    {
        for (var i = 0; i < samples; i++)
            result[i] = InverseDistributionFunction(source.NextDouble());
        return result;
    }

    public virtual double[] Generate(int samples, double[] result, Random source)
    {
        for (var i = 0; i < samples; i++)
            result[i] = InverseDistributionFunction(source.NextDouble());
        return result;
    }

    public virtual int Generate(Random source)
    {
        return InverseDistributionFunction(source.NextDouble());
    }

    double[] IRandomNumberGenerator<double>.Generate(int samples)
    {
        return Generate(samples, new double[samples]);
    }

    double[] ISampleableDistribution<double>.Generate(int samples, Random source)
    {
        return Generate(samples, new double[samples], source);
    }

    double ISampleableDistribution<double>.Generate(double result)
    {
        return Generate();
    }

    int ISampleableDistribution<int>.Generate(int result)
    {
        return Generate();
    }

    double ISampleableDistribution<double>.Generate(double result, Random source)
    {
        return Generate();
    }

    int ISampleableDistribution<int>.Generate(int result, Random source)
    {
        return Generate();
    }

    double IRandomNumberGenerator<double>.Generate()
    {
        return Generate();
    }

    double ISampleableDistribution<double>.Generate(Random source)
    {
        return Generate(source);
    }

    double IDistribution<int>.ProbabilityFunction(int x)
    {
        return ProbabilityMassFunction(x);
    }

    double IDistribution<int>.LogProbabilityFunction(int x)
    {
        return LogProbabilityMassFunction(x);
    }

    double IDistribution<double[]>.DistributionFunction(double[] x)
    {
        return (this as IDistribution).DistributionFunction(x);
    }

    double IDistribution<double[]>.ProbabilityFunction(double[] x)
    {
        return (this as IDistribution).ProbabilityFunction(x);
    }

    double IDistribution<double[]>.LogProbabilityFunction(double[] x)
    {
        return (this as IDistribution).LogProbabilityFunction(x);
    }

    double IDistribution<double[]>.ComplementaryDistributionFunction(double[] x)
    {
        return (this as IDistribution).ComplementaryDistributionFunction(x);
    }

    double IDistribution<double>.DistributionFunction(double x)
    {
        return DistributionFunction((int)x);
    }

    double IDistribution<double>.ProbabilityFunction(double x)
    {
        return ProbabilityMassFunction((int)x);
    }

    double IDistribution<double>.LogProbabilityFunction(double x)
    {
        return LogProbabilityMassFunction((int)x);
    }

    double IDistribution<double>.ComplementaryDistributionFunction(double x)
    {
        return ComplementaryDistributionFunction((int)x);
    }

    double IUnivariateDistribution<double>.InverseDistributionFunction(double p)
    {
        return InverseDistributionFunction(p);
    }

    double IUnivariateDistribution<double>.HazardFunction(double x)
    {
        return HazardFunction((int)x);
    }

    double IUnivariateDistribution<double>.CumulativeHazardFunction(double x)
    {
        return CumulativeHazardFunction((int)x);
    }
}