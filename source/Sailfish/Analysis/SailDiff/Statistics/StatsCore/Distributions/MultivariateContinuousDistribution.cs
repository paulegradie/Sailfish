using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Sampling;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Serializable]
public abstract class MultivariateContinuousDistribution : DistributionBase
{
    [NonSerialized]
    private MetropolisHasting<double> generator;

    protected MultivariateContinuousDistribution(int dimension)
    {
        Dimension = dimension;
    }

    public int Dimension { get; }

    public abstract double[] Mean { get; }

    public abstract double[] Variance { get; }

    public abstract double[,] Covariance { get; }

    public virtual double[] Mode => Mean;

    public virtual double[] Median => Mean;
}