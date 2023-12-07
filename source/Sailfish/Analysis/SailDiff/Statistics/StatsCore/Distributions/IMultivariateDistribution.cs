using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public interface IMultivariateDistribution : IDistribution, ICloneable
{
    int Dimension { get; }

    double[] Mean { get; }

    double[] Median { get; }

    double[] Mode { get; }

    double[] Variance { get; }

    double[,] Covariance { get; }
}

public interface IMultivariateDistribution<in TObservation> :
    IDistribution<TObservation>,
    IDistribution,
    ICloneable
{
    int Dimension { get; }

    double[] Mean { get; }

    double[] Median { get; }

    double[] Mode { get; }

    double[] Variance { get; }

    double[,] Covariance { get; }
}