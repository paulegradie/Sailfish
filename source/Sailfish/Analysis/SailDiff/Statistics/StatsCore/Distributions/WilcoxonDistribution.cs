using System;
using System.Linq;
using System.Threading.Tasks;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionFactories;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

internal sealed class WilcoxonDistribution : UnivariateContinuousDistribution
{
    private readonly NormalDistribution approximation;

    internal WilcoxonDistribution(double[] ranks, bool exact)
    {
        Exact = exact;
        Correction = ContinuityCorrection.Midpoint;

        var mean = ranks.Length * (ranks.Length + 1.0) / 4.0;
        var stdDev = Math.Sqrt(ranks.Length * (ranks.Length + 1.0) * (2.0 * ranks.Length + 1.0) / 24.0);
        approximation = NormalDistributionFactory.Create(mean, stdDev);
        Table = null;
        if (!exact) return;

        ranks = ranks.Get(ranks.Find(x => x != 0.0));
        var exactN = (long)Math.Pow(2.0, ranks.Length);
        var source = Combinatorics
            .Sequences(ranks.Length)
            .Zip(exactN.EnumerableRange(), (Func<int[], long, Tuple<int[], long>>)((c, i) => new Tuple<int[], long>(c, i)));
        Table = new double[exactN];
        var body = (Action<Tuple<int[], long>>)(item =>
        {
            var signs = item.Item1;
            var index1 = item.Item2;
            for (var index2 = 0; index2 < signs.Length; ++index2) signs[index2] = Math.Sign(signs[index2] * 2 - 1);

            Table[index1] = WPositive(signs, ranks);
        });
        Parallel.ForEach(source, body);
        Array.Sort(Table);
    }

    public bool Exact { get; }

    public double[]? Table { get; }

    public ContinuityCorrection Correction { get; set; }

    public override double Mean => approximation.Mean;


    public override DoubleRange Support => Exact ? new DoubleRange(0.0, double.PositiveInfinity) : approximation.Support;


    public static double WPositive(int[] signs, double[] ranks)
    {
        var num = 0.0;
        for (var index = 0; index < signs.Length; ++index)
            if (signs[index] > 0)
                num += ranks[index];
        return num;
    }

    protected override double InnerDistributionFunction(double x)
    {
        if (Exact)
            return ExactMethod(x, Table!);
        if (x > Mean)
            x -= 0.5;
        else
            x += 0.5;

        return approximation.DistributionFunction(x);
    }

    protected override double InnerComplementaryDistributionFunction(double x)
    {
        if (Exact)
            return ExactComplement(x, Table!);
        if (x > Mean)
            x -= 0.5;
        else
            x += 0.5;

        return approximation.ComplementaryDistributionFunction(x);
    }

    protected override double InnerInverseDistributionFunction(double p)
    {
        return Exact ? base.InnerInverseDistributionFunction(p) : approximation.InverseDistributionFunction(p);
    }

    protected override double InnerProbabilityDensityFunction(double x)
    {
        return Exact ? Count(x, Table!) / (double)Table!.Length : approximation.ProbabilityDensityFunction(x);
    }

    protected override double InnerLogProbabilityDensityFunction(double x)
    {
        return Exact ? Math.Log(Count(x, Table!)) - Math.Log(Table!.Length) : approximation.LogProbabilityDensityFunction(x);
    }

    public override string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(formatProvider, "W+(x; R)");
    }

    internal static double ExactMethod(double x, double[] table)
    {
        for (var index = 0; index < table.Length; ++index)
            if (x < table[index])
                return index / (double)table.Length;
        return 1.0;
    }

    internal static double ExactComplement(double x, double[] table)
    {
        for (var index = table.Length - 1; index >= 0; --index)
            if (table[index] < x)
                return (table.Length - index - 1) / (double)table.Length;
        return 1.0;
    }

    internal static int Count(double x, double[] table)
    {
        return table.Count(t => Math.Abs(t - x) < 0.00000000000001);
    }
}