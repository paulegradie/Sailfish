using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;
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

    void Fit(Array observations);

    void Fit(Array observations, double[] weights);

    void Fit(Array observations, int[] weights);

    void Fit(Array observations, IFittingOptions options);

    void Fit(Array observations, double[] weights, IFittingOptions options);

    void Fit(Array observations, int[] weights, IFittingOptions options);
}