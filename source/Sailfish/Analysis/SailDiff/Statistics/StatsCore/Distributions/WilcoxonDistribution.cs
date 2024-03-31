using System;
using System.Linq;
using System.Threading.Tasks;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

[Serializable]
public class WilcoxonDistribution : UnivariateContinuousDistribution
{
    private NormalDistribution approximation;

    public WilcoxonDistribution([PositiveInteger] int n)
    {
        Init(n, null, new bool?());
    }

    public WilcoxonDistribution(double[] ranks, bool? exact = null)
    {
        Init(ranks.Length, ranks, exact);
    }

    public int NumberOfSamples { get; private set; }

    public bool Exact { get; private set; }

    public double[] Table { get; private set; }

    public ContinuityCorrection Correction { get; set; }

    public override double Mean => approximation.Mean;


    public override DoubleRange Support => Exact ? new DoubleRange(0.0, double.PositiveInfinity) : approximation.Support;

    private void Init(int n, double[]? ranks, bool? exact)
    {
        NumberOfSamples = n > 0 ? n : throw new ArgumentOutOfRangeException(nameof(n), "The number of samples must be positive.");
        var mean = n * (n + 1.0) / 4.0;
        var stdDev = Math.Sqrt(n * (n + 1.0) * (2.0 * n + 1.0) / 24.0);
        var flag = ranks != null;
        Exact = flag && n <= 12;
        if (exact.HasValue)
        {
            if (exact.Value && !flag)
                throw new ArgumentException(nameof(exact), "Cannot use exact method if rank vectors are not specified.");
            Exact = exact.Value;
        }

        if (flag && Exact)
            InitExactMethod(ranks);
        approximation = new NormalDistribution(mean, stdDev);
    }

    private void InitExactMethod(double[] ranks)
    {
        ranks = ranks.Get(ranks.Find(x => x != 0.0));
        var n = (long)Math.Pow(2.0, ranks.Length);
        var source = Combinatorics.Sequences(ranks.Length)
            .Zip(InternalOps.EnumerableRange(n), (Func<int[], long, Tuple<int[], long>>)((c, i) => new Tuple<int[], long>(c, i)));
        Table = new double[n];
        var body = (Action<Tuple<int[], long>>)(item =>
        {
            var signs = item.Item1;
            var index1 = item.Item2;
            for (var index2 = 0; index2 < signs.Length; ++index2)
                signs[index2] = Math.Sign(signs[index2] * 2 - 1);
            Table[index1] = WPositive(signs, ranks);
        });
        Parallel.ForEach(source, body);
        Array.Sort(Table);
    }

    public static double WPositive(int[] signs, double[] ranks)
    {
        var num = 0.0;
        for (var index = 0; index < signs.Length; ++index)
            if (signs[index] > 0)
                num += ranks[index];
        return num;
    }

    public override object Clone()
    {
        return new WilcoxonDistribution(NumberOfSamples)
        {
            Exact = Exact,
            Table = Table,
            NumberOfSamples = NumberOfSamples,
            approximation = (NormalDistribution)approximation.Clone()
        };
    }

    protected internal override double InnerDistributionFunction(double x)
    {
        if (Exact)
            return ExactMethod(x, Table);
        if (Correction == ContinuityCorrection.Midpoint)
        {
            if (x > Mean)
                x -= 0.5;
            else
                x += 0.5;
        }
        else if (Correction == ContinuityCorrection.KeepInside)
        {
            x += 0.5;
        }

        return approximation.DistributionFunction(x);
    }

    protected internal override double InnerComplementaryDistributionFunction(double x)
    {
        if (Exact)
            return ExactComplement(x, Table);
        if (Correction == ContinuityCorrection.Midpoint)
        {
            if (x > Mean)
                x -= 0.5;
            else
                x += 0.5;
        }
        else if (Correction == ContinuityCorrection.KeepInside)
        {
            x -= 0.5;
        }

        return approximation.ComplementaryDistributionFunction(x);
    }

    protected internal override double InnerInverseDistributionFunction(double p)
    {
        return Exact ? base.InnerInverseDistributionFunction(p) : approximation.InverseDistributionFunction(p);
    }

    protected internal override double InnerProbabilityDensityFunction(double x)
    {
        return Exact ? Count(x, Table) / (double)Table.Length : approximation.ProbabilityDensityFunction(x);
    }

    protected internal override double InnerLogProbabilityDensityFunction(double x)
    {
        return Exact ? Math.Log(Count(x, Table)) - Math.Log(Table.Length) : approximation.LogProbabilityDensityFunction(x);
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