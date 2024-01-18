using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public interface IFittable<in TObservations, in TOptions> : IFittable<TObservations> where TOptions : class, IFittingOptions
{
    void Fit(TObservations[] observations, double[] weights = null, TOptions options = null);
}

public interface IFittable<in TObservations>
{
    //void Fit(TObservations[] observations);

    void Fit(TObservations[] observations, double[] weights);
}