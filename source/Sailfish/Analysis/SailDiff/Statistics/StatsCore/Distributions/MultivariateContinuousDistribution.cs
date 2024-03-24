using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Sampling;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Serializable]
public abstract class MultivariateContinuousDistribution :
    DistributionBase,
    IMultivariateDistribution,
    IMultivariateDistribution<double[]>,
    IFittableDistribution<double[]>,
    ISampleableDistribution<double[]>,
    IRandomNumberGenerator<double[]>
{
    [NonSerialized]
    private MetropolisHasting<double> generator;

    protected MultivariateContinuousDistribution(int dimension)
    {
        Dimension = dimension;
    }

    public virtual void Fit(double[][] observations)
    {
        Fit(observations, (IFittingOptions)null);
    }

    public virtual void Fit(double[][] observations, double[]? weights)
    {
        Fit(observations, weights, null);
    }

    public int Dimension { get; }

    public abstract double[] Mean { get; }

    public abstract double[] Variance { get; }

    public abstract double[,] Covariance { get; }

    public virtual double[] Mode => Mean;

    public virtual double[] Median => Mean;

    double IDistribution.ProbabilityFunction(double[] x)
    {
        return ProbabilityDensityFunction(x);
    }

    double IDistribution.LogProbabilityFunction(double[] x)
    {
        return LogProbabilityDensityFunction(x);
    }

    public virtual double DistributionFunction(params double[] x)
    {
        return x != null ? InnerDistributionFunction(x) : throw new ArgumentNullException(nameof(x));
    }

    public virtual double ComplementaryDistributionFunction(params double[] x)
    {
        return x != null ? InnerComplementaryDistributionFunction(x) : throw new ArgumentNullException(nameof(x));
    }

    double IDistribution<double[]>.ProbabilityFunction(double[] x)
    {
        return ProbabilityDensityFunction(x);
    }

    double IDistribution<double[]>.LogProbabilityFunction(double[] x)
    {
        return LogProbabilityDensityFunction(x);
    }

    public double[][] Generate(int samples)
    {
        return Generate(samples, Generator.Random);
    }

    public double[][] Generate(int samples, double[][] result)
    {
        return Generate(samples, result, Generator.Random);
    }

    public double[] Generate()
    {
        return Generate(Generator.Random);
    }

    public double[] Generate(double[] result)
    {
        return Generate(result, Generator.Random);
    }

    public double[][] Generate(int samples, Random source)
    {
        return Generate(samples, InternalOps.Create<double>(samples, Dimension), source);
    }

    public virtual double[][] Generate(int samples, double[][] result, Random source)
    {
        if (generator.RandomSource != source)
            generator.RandomSource = source;
        return Generate(samples).Round(result);
    }

    public double[] Generate(Random source)
    {
        return Generate(1, source)[0];
    }

    public double[] Generate(double[] result, Random source)
    {
        return Generate(1, new double[1][]
        {
            result
        }, source)[0];
    }

    protected internal virtual double InnerDistributionFunction(double[] x)
    {
        throw new NotImplementedException();
    }

    public virtual double ProbabilityDensityFunction(params double[] x)
    {
        return x != null ? InnerProbabilityDensityFunction(x) : throw new ArgumentNullException(nameof(x));
    }

    protected internal virtual double InnerProbabilityDensityFunction(double[] x)
    {
        throw new NotImplementedException();
    }

    public virtual double LogProbabilityDensityFunction(params double[] x)
    {
        return InnerLogProbabilityDensityFunction(x);
    }

    protected internal virtual double InnerLogProbabilityDensityFunction(params double[] x)
    {
        return x != null ? Math.Log(ProbabilityDensityFunction(x)) : throw new ArgumentNullException(nameof(x));
    }

    protected internal virtual double InnerComplementaryDistributionFunction(params double[] x)
    {
        return 1.0 - DistributionFunction(x);
    }

    public virtual void Fit(double[][] observations, int[] weights)
    {
        Fit(observations, weights, null);
    }

    public virtual void Fit(double[][] observations, IFittingOptions options)
    {
        Fit(observations, (double[])null, options);
    }

    public virtual void Fit(double[][] observations, double[]? weights, IFittingOptions options)
    {
        throw new NotSupportedException();
    }

    public virtual void Fit(double[][] observations, int[] weights, IFittingOptions options)
    {
        if (weights != null)
            throw new NotSupportedException();
        Fit(observations, (double[])null, options);
    }
}