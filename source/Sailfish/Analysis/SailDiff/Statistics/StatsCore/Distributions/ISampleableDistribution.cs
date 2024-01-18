using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public interface ISampleableDistribution<TObservations> :
    IDistribution<TObservations>, StatsCore.IRandomNumberGenerator<TObservations>
{
    TObservations Generate(TObservations result);

    TObservations Generate(TObservations result, Random source);

    TObservations[] Generate(int samples, Random source);

    TObservations[] Generate(int samples, TObservations[] result, Random source);

    TObservations Generate(Random source);
}