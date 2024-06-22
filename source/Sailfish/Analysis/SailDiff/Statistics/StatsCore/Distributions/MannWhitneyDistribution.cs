using System;
using System.Linq;
using System.Threading.Tasks;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionFactories;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

internal sealed class MannWhitneyDistribution : UnivariateContinuousDistribution
{
    private readonly NormalDistribution approximation;

    internal MannWhitneyDistribution(double[] ranks, int rank1Length, int rank2Length)
    {
        var num1 = rank1Length + rank2Length;
        NumberOfSamples1 = rank1Length;
        NumberOfSamples2 = rank2Length;
        var mean = rank1Length * rank2Length / 2.0;
        Exact = rank1Length <= 30 && rank2Length <= 30;

        var corrections = Corrections(ranks);
        var stdDev = Math.Sqrt(rank1Length * rank2Length / 12.0 * (num1 + 1.0 - corrections));
        if (Exact)
        {
            var k = Math.Min(NumberOfSamples1, NumberOfSamples2);
            var n = (long)Specials.Binomial(NumberOfSamples1 + NumberOfSamples2, k);
            Table = new double[n];
            Parallel
                .ForEach(ranks.Combinations(k).Zip(Vector.Range(n), (Func<double[], long, Tuple<double[], long>>)((c, i) => new Tuple<double[], long>(c, i))),
                    (Action<Tuple<double[], long>>)(i => Table[i.Item2] = MannWhitneyU(i.Item1)));
            Array.Sort(Table);
        }

        approximation = NormalDistributionFactory.Create(mean, stdDev);
        Correction = ContinuityCorrection.Midpoint;
    }

    public int NumberOfSamples1 { get; }

    public int NumberOfSamples2 { get; }

    public ContinuityCorrection Correction { get; init; }

    public bool Exact { get; }

    public double[]? Table { get; }

    public override double Mean => approximation.Mean;


    public override DoubleRange Support => new(double.NegativeInfinity, double.PositiveInfinity);

    private static double Corrections(double[] ranks)
    {
        var length = ranks.Length;
        if (length <= 1)
            throw new ArgumentOutOfRangeException(nameof(ranks));
        var numArray = ranks.Ties();
        var num1 = (from t in numArray let num2 = t * t * t select (double)num2 - t).Sum();

        return num1 / (length * (length - 1));
    }


    protected override double InnerComplementaryDistributionFunction(double x)
    {
        return NumberOfSamples1 <= NumberOfSamples2 ? ComplementaryDistributionFunction(x) : DistributionFunction(x);
    }

    public override double DistributionFunction(double x)
    {
        if (Exact) return WilcoxonDistribution.ExactMethod(x, Table!);
        if (x > Mean)
            x -= 0.5;
        else
            x += 0.5;

        return approximation.DistributionFunction(x);
    }

    private new double ComplementaryDistributionFunction(double x)
    {
        if (Exact)
            return WilcoxonDistribution.ExactComplement(x, Table!);

        switch (Correction)
        {
            case ContinuityCorrection.Midpoint when x > Mean:
                x -= 0.5;
                break;
            case ContinuityCorrection.Midpoint:
                x += 0.5;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return approximation.ComplementaryDistributionFunction(x);
    }

    private static double MannWhitneyU(double[] ranks)
    {
        var length = ranks.Length;
        return ranks.Sum() - length * (length + 1.0) / 2.0;
    }

    protected override double InnerProbabilityDensityFunction(double x)
    {
        return Exact ? WilcoxonDistribution.Count(x, Table!) / (double)Table!.Length : approximation.ProbabilityDensityFunction(x);
    }

    protected override double InnerLogProbabilityDensityFunction(double x)
    {
        return Exact ? Math.Log(WilcoxonDistribution.Count(x, Table!)) - Math.Log(Table!.Length) : approximation.ProbabilityDensityFunction(x);
    }

    protected override double InnerInverseDistributionFunction(double p)
    {
        if (!Exact) return approximation.InverseDistributionFunction(p);
        if (NumberOfSamples1 <= NumberOfSamples2)
            return base.InnerInverseDistributionFunction(p);
        var num1 = 0.0;
        var num2 = 0.0;
        var num3 = base.DistributionFunction(0.0);
        if (num3 > p)
        {
            for (; num3 > p && !double.IsInfinity(num1); num3 = base.DistributionFunction(num2))
            {
                num1 = num2;
                num2 = 2.0 * num2 + 1.0;
            }
        }
        else
        {
            if (num3 >= p)
                return 0.0;
            for (; num3 < p && !double.IsInfinity(num2); num3 = base.DistributionFunction(num1))
            {
                num2 = num1;
                num1 = 2.0 * num1 - 1.0;
            }
        }

        if (double.IsNegativeInfinity(num1))
            num1 = double.MinValue;
        if (double.IsPositiveInfinity(num2))
            num2 = double.MaxValue;
        return BrentSearch.Find(base.DistributionFunction, p, num1, num2);
    }

    public override string ToString(string? format, IFormatProvider? formatProvider)
    {
        var num = NumberOfSamples1;
        var str1 = num.ToString(format, formatProvider);
        num = NumberOfSamples2;
        var str2 = num.ToString(format, formatProvider);
        return string.Format(formatProvider, "MannWhitney(u; n1 = {0}, n2 = {1})", str1, str2);
    }
}