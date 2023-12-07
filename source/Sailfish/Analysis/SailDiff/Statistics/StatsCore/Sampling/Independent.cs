using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;
using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Sampling;

[Serializable]
public class Independent<TDistribution, TObservation> :
    Independent<TDistribution>,
    IMultivariateDistribution<TObservation[]>,
    IDistribution<TObservation[]>,
    IDistribution,
    ICloneable,
    IFittableDistribution<TObservation[], IndependentOptions>,
    IFittable<TObservation[], IndependentOptions>,
    IFittable<TObservation[]>,
    IFittableDistribution<TObservation[]>,
    ISampleableDistribution<TObservation[]>, Distributions.IRandomNumberGenerator<TObservation[]> where TDistribution : IUnivariateDistribution<TObservation>, IUnivariateDistribution
{
    public Independent(int dimensions, Func<int, TDistribution> initializer)
        : base(dimensions, initializer)
    {
    }

    public Independent(int dimensions, Func<TDistribution> initializer)
        : base(dimensions, initializer)
    {
    }

    public Independent(int dimensions, TDistribution component)
        : base(dimensions, component)
    {
    }

    public Independent(params TDistribution[] components)
        : base(components)
    {
    }

    public void Fit(TObservation[][] observations, double[] weights)
    {
        Fit(observations, weights, null);
    }

    public void Fit(TObservation[][] observations, double[] weights, IndependentOptions options)
    {
        if (options != null)
        {
            if (!options.Transposed)
                observations = observations.Transpose();
            if (options.InnerOptions != null)
                for (var index = 0; index < Components.Length; ++index)
                    Components[index].Fit(observations[index], weights, options.InnerOptions[index]);
            else
                for (var index = 0; index < Components.Length; ++index)
                    Components[index].Fit(observations[index], weights, options.InnerOption);
        }
        else
        {
            observations = observations.Transpose();
            for (var index = 0; index < Components.Length; ++index)
                ((IFittable<TObservation>)Components[index]).Fit(observations[index], weights);
        }

        Reset();
    }

    public double ProbabilityFunction(TObservation[] x)
    {
        var num = 1.0;
        for (var index = 0; index < Components.Length; ++index)
            num *= Components[index].ProbabilityFunction(x[index]);
        return num;
    }

    public double DistributionFunction(TObservation[] x)
    {
        var num = 1.0;
        for (var index = 0; index < Components.Length; ++index)
            num *= Components[index].DistributionFunction(x[index]);
        return num;
    }

    public double ComplementaryDistributionFunction(TObservation[] x)
    {
        var num = 1.0;
        for (var index = 0; index < Components.Length; ++index)
            num *= Components[index].ComplementaryDistributionFunction(x[index]);
        return num;
    }

    public double LogProbabilityFunction(TObservation[] x)
    {
        var num = 0.0;
        for (var index = 0; index < Components.Length; ++index)
            num += Components[index].LogProbabilityFunction(x[index]);
        return num;
    }

    public override object Clone()
    {
        var distributionArray = new TDistribution[Components.Length];
        for (var index = 0; index < distributionArray.Length; ++index)
            distributionArray[index] = (TDistribution)Components[index].Clone();
        return new Independent<TDistribution, TObservation>(distributionArray);
    }

    TObservation[][] Distributions.IRandomNumberGenerator<TObservation[]>.Generate(int samples)
    {
        return Generate(samples, Ops.InternalOps.Zeros<TObservation>(samples, Components.Length));
    }

    TObservation[] Distributions.IRandomNumberGenerator<TObservation[]>.Generate()
    {
        return Generate(new TObservation[Components.Length]);
    }

    public TObservation[] Generate(TObservation[] result)
    {
        return Generate(result, Generator.Random);
    }

    public TObservation[] Generate(TObservation[] result, Random source)
    {
        for (var index = 0; index < Components.Length; ++index)
            result[index] = ((ISampleableDistribution<TObservation>)Components[index]).Generate(source);
        return result;
    }

    public TObservation[][] Generate(int samples)
    {
        throw new NotImplementedException();
    }

    public TObservation[][] Generate(int samples, TObservation[][] result)
    {
        return Generate(samples, result, Generator.Random);
    }

    public TObservation[] Generate()
    {
        throw new NotImplementedException();
    }

    public TObservation[][] Generate(int samples, TObservation[][] result, Random source)
    {
        for (var index = 0; index < Components.Length; ++index)
            result.SetColumn(index, ((ISampleableDistribution<TObservation>)Components[index]).Generate(samples, source));
        return result;
    }

    TObservation[][] ISampleableDistribution<TObservation[]>.Generate(int samples, Random source)
    {
        return Generate(samples, Ops.InternalOps.Zeros<TObservation>(samples, Components.Length), source);
    }

    TObservation[] ISampleableDistribution<TObservation[]>.Generate(Random source)
    {
        return Generate(new TObservation[Components.Length], source);
    }
}