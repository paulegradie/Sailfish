using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public interface IFittableDistribution<in TObservations, in TOptions> :  IFittableDistribution<TObservations>
    where TOptions : class, IFittingOptions;

public interface IFittableDistribution<in TObservations> :
    IDistribution<TObservations>;