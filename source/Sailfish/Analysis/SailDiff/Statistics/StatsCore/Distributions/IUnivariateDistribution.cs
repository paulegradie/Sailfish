namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public interface IUnivariateDistribution<TObservation> : IDistribution<TObservation>
{
    double Mean { get; }

    double Variance { get; }

    double Median { get; }

    double Mode { get; }

    double Entropy { get; }

    DoubleRange Support { get; }

    TObservation InverseDistributionFunction(double p);

    double HazardFunction(TObservation x);

    double CumulativeHazardFunction(TObservation x);
}

public interface IUnivariateDistribution : IDistribution
{
    double Mean { get; }

    double Variance { get; }

    double Median { get; }

    double Mode { get; }

    double Entropy { get; }

    DoubleRange Support { get; }

    DoubleRange Quartiles { get; }

    DoubleRange GetRange(double percentile);

    double DistributionFunction(double x);

    double DistributionFunction(double a, double b);

    double ProbabilityFunction(double x);

    double LogProbabilityFunction(double x);

    double InverseDistributionFunction(double p);

    double ComplementaryDistributionFunction(double x);

    double HazardFunction(double x);

    double CumulativeHazardFunction(double x);

    double LogCumulativeHazardFunction(double x);

    double QuantileDensityFunction(double p);
}