using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore;

public interface IDistribution<in TObservation> : IDistribution
{
    double DistributionFunction(TObservation x);

    double ProbabilityFunction(TObservation x);

    double LogProbabilityFunction(TObservation x);

    double ComplementaryDistributionFunction(TObservation x);
}

public interface IDistribution : ICloneable
{
    double DistributionFunction(params double[] x);

    double ProbabilityFunction(params double[] x);

    double LogProbabilityFunction(params double[] x);

    double ComplementaryDistributionFunction(params double[] x);
}