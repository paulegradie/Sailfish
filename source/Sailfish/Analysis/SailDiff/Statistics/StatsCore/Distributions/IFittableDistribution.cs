using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;
using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public interface IFittableDistribution<in TObservations, in TOptions> :
    IFittable<TObservations, TOptions>,
    IFittable<TObservations>,
    IFittableDistribution<TObservations>,
    IDistribution<TObservations>,
    IDistribution,
    ICloneable
    where TOptions : class, IFittingOptions
{
}

public interface IFittableDistribution<in TObservations> :
    IFittable<TObservations>,
    IDistribution<TObservations>,
    IDistribution,
    ICloneable
{
}