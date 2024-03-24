namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public interface IMultivariateDistribution : IDistribution
{
    int Dimension { get; }

    double[] Mean { get; }

    double[] Median { get; }

    double[] Mode { get; }

    double[] Variance { get; }

    double[,] Covariance { get; }
}

public interface IMultivariateDistribution<in TObservation> : IDistribution<TObservation>
{
    int Dimension { get; }

    double[] Mean { get; }

    double[] Median { get; }

    double[] Mode { get; }

    double[] Variance { get; }

    double[,] Covariance { get; }
}