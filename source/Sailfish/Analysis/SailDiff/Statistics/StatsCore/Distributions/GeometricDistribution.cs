using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.Options;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Serializable]
public class GeometricDistribution([Unit] double probabilityOfSuccess) :
    UnivariateDiscreteDistribution,
    IFittableDistribution<double, IFittingOptions>,
    ISampleableDistribution<int>,
    IRandomNumberGenerator<int>
{
    public double ProbabilityOfSuccess { get; private set; } = probabilityOfSuccess >= 0.0 && probabilityOfSuccess <= 1.0
            ? probabilityOfSuccess
            : throw new ArgumentOutOfRangeException(nameof(probabilityOfSuccess), "A probability must be between 0 and 1.");

    public override double Mean => (1.0 - ProbabilityOfSuccess) / ProbabilityOfSuccess;

    public override double Mode => 0.0;

    public override double Median => Math.Ceiling(-1.0 / Math.Log(1.0 - ProbabilityOfSuccess, 2.0)) - 1.0;

    public override double Variance => (1.0 - ProbabilityOfSuccess) / (ProbabilityOfSuccess * ProbabilityOfSuccess);

    public override double Entropy => (-(1.0 - ProbabilityOfSuccess) * Math.Log(1.0 - ProbabilityOfSuccess, 2.0) - ProbabilityOfSuccess * Math.Log(ProbabilityOfSuccess, 2.0)) /
                                      ProbabilityOfSuccess;

    public override IntRange Support => new(0, int.MaxValue);

    public override void Fit(double[] observations, double[]? weights, IFittingOptions? options)
    {
        if (options != null)
            throw new ArgumentException("No options may be specified.");
        ProbabilityOfSuccess = 1.0 / (1.0 - (weights != null ? observations.WeightedMean(weights) : observations.Mean()));
    }

    public override object Clone()
    {
        return new GeometricDistribution(ProbabilityOfSuccess);
    }

    public override int Generate(Random source)
    {
        return (int)Random(ProbabilityOfSuccess, source);
    }

    public override int[] Generate(int samples, int[] result, Random source)
    {
        return Random(ProbabilityOfSuccess, samples, result, source);
    }

    protected internal override double InnerDistributionFunction(int k)
    {
        return 1.0 - Math.Pow(1.0 - ProbabilityOfSuccess, k + 1);
    }

    protected internal override double InnerProbabilityMassFunction(int k)
    {
        return Math.Pow(1.0 - ProbabilityOfSuccess, k) * ProbabilityOfSuccess;
    }

    protected internal override double InnerLogProbabilityMassFunction(int k)
    {
        return k * Math.Log(1.0 - ProbabilityOfSuccess) + Math.Log(ProbabilityOfSuccess);
    }

    protected override int InnerInverseDistributionFunction(double p)
    {
        return (int)Math.Ceiling(Specials.Log1m(p) / Specials.Log1m(ProbabilityOfSuccess)) - 1;
    }

    public override double[] Generate(int samples, double[] result, Random source)
    {
        return Random(ProbabilityOfSuccess, samples, result, source);
    }

    public static double Random(double p, Random source)
    {
        return Math.Floor(Specials.Log1m(source.NextDouble()) / Specials.Log1m(p));
    }

    public static double[] Random(double p, int samples, double[] result, Random source)
    {
        for (var index = 0; index < samples; ++index)
            result[index] = Math.Floor(Specials.Log1m(source.NextDouble()) / Specials.Log1m(p));
        return result;
    }

    public static int[] Random(double p, int samples, int[] result, Random source)
    {
        for (var index = 0; index < samples; ++index)
            result[index] = (int)Math.Floor(Specials.Log1m(source.NextDouble()) / Specials.Log1m(p));
        return result;
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        return string.Format(formatProvider, "Geometric(x; p = {0})", ProbabilityOfSuccess.ToString(format, formatProvider));
    }
}