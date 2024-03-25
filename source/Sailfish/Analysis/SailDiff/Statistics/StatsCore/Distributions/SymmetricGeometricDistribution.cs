using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Serializable]
public class SymmetricGeometricDistribution : UnivariateDiscreteDistribution
{
    private readonly double lnconstant;

    public SymmetricGeometricDistribution([Unit] double probabilityOfSuccess)
    {
        ProbabilityOfSuccess = probabilityOfSuccess is >= 0.0 and <= 1.0
            ? probabilityOfSuccess
            : throw new ArgumentOutOfRangeException(nameof(probabilityOfSuccess), "A probability must be between 0 and 1.");
        lnconstant = Math.Log(ProbabilityOfSuccess) - Math.Log(2.0 * (1.0 - ProbabilityOfSuccess));
    }

    public double ProbabilityOfSuccess { get; }

    public override IntRange Support => new(int.MinValue, int.MaxValue);

    public override double Mean => 0.0;

    public override double Variance => (2.0 - ProbabilityOfSuccess) * (1.0 - ProbabilityOfSuccess) / (ProbabilityOfSuccess * ProbabilityOfSuccess);

    public override double Entropy => throw new NotSupportedException();

    protected internal override double InnerDistributionFunction(int k)
    {
        throw new NotSupportedException();
    }

    protected internal override double InnerProbabilityMassFunction(int k)
    {
        return k == 0 ? ProbabilityOfSuccess : Math.Exp(lnconstant) * Math.Pow(1.0 - ProbabilityOfSuccess, Math.Abs(k) - 1);
    }

    protected internal override double InnerLogProbabilityMassFunction(int k)
    {
        return k == 0 ? Math.Log(ProbabilityOfSuccess) : lnconstant + (Math.Abs(k) - 1) * Math.Log(1.0 - ProbabilityOfSuccess);
    }

    public override int Generate(Random source)
    {
        return Math.Sign(source.NextDouble() - 0.5) * (int)GeometricDistribution.Random(ProbabilityOfSuccess, source);
    }

    public override double[] Generate(int samples, double[] result, Random source)
    {
        GeometricDistribution.Random(ProbabilityOfSuccess, samples, result, source);
        for (var index = 0; index < samples; ++index)
            result[index] *= Math.Sign(source.NextDouble() - 0.5);
        return result;
    }

    public override int[] Generate(int samples, int[] result, Random source)
    {
        GeometricDistribution.Random(ProbabilityOfSuccess, samples, result, source);
        for (var index = 0; index < samples; ++index)
            result[index] *= Math.Sign(source.NextDouble() - 0.5);
        return result;
    }

    public override object Clone()
    {
        return new SymmetricGeometricDistribution(ProbabilityOfSuccess);
    }

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        return string.Format(formatProvider, "SymmetricGeometric(x; p = {0})", ProbabilityOfSuccess.ToString(format, formatProvider));
    }
}