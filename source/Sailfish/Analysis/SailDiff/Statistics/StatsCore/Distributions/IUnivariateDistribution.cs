namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public interface IUnivariateDistribution
{
    double Mean { get; }

    double DistributionFunction(double x);

    double LogProbabilityFunction(double x);
}