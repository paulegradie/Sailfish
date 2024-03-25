using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public interface ISampleableDistribution<out TObservations>
{
    TObservations[] Generate(int samples, Random source);
}