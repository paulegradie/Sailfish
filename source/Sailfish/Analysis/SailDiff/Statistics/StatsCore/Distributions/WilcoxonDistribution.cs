using System;
using System.Linq;
using System.Threading.Tasks;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionFactories;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

internal sealed class WilcoxonDistribution : UnivariateContinuousDistribution
{
    private readonly NormalDistribution _approximation;

    internal WilcoxonDistribution(double[] ranks, bool exact)
    {
        Exact = exact;
        Correction = ContinuityCorrection.Midpoint;

        var mean = ranks.Length * (ranks.Length + 1.0) / 4.0;
        var stdDev = Math.Sqrt(ranks.Length * (ranks.Length + 1.0) * (2.0 * ranks.Length + 1.0) / 24.0);
        _approximation = NormalDistributionFactory.Create(mean, stdDev);
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

    public override double Mean => _approximation.Mean;


    public override DoubleRange Support => Exact ? new DoubleRange(0.0, double.PositiveInfinity) : _approximation.Support;


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

        return _approximation.DistributionFunction(x);
    }

    protected override double InnerComplementaryDistributionFunction(double x)
    {
        if (Exact)
            return ExactComplement(x, Table!);
        if (x > Mean)
            x -= 0.5;
        else
            x += 0.5;

        return _approximation.ComplementaryDistributionFunction(x);
    }

    protected override double InnerInverseDistributionFunction(double p)
    {
        return Exact ? base.InnerInverseDistributionFunction(p) : _approximation.InverseDistributionFunction(p);
    }

    protected override double InnerProbabilityDensityFunction(double x)
    {
        return Exact ? Count(x, Table!) / (double)Table!.Length : _approximation.ProbabilityDensityFunction(x);
    }

    protected override double InnerLogProbabilityDensityFunction(double x)
    {
        return Exact ? Math.Log(Count(x, Table!)) - Math.Log(Table!.Length) : _approximation.LogProbabilityDensityFunction(x);
    }

    public override string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(formatProvider, "W+(x; R)");
    }

    // Returns the count of table entries strictly greater than `x` divided by table.Length —
    // equivalently P(U ≤ x) on a sorted ascending table. Was an O(n) linear scan despite the
    // table being sorted; converted to binary search via Array.BinarySearch.
    internal static double ExactMethod(double x, double[] table)
    {
        var idx = Array.BinarySearch(table, x);
        // BinarySearch returns the index of any matching entry, or the bitwise-complement of
        // the first entry strictly greater than `x`. We want the count of entries ≤ x. When
        // there are matches, walk forward to include all equal entries (table may contain
        // duplicates from the enumeration that built it).
        int countLessOrEqual;
        if (idx >= 0)
        {
            countLessOrEqual = idx + 1;
            while (countLessOrEqual < table.Length && table[countLessOrEqual] == x)
                countLessOrEqual++;
        }
        else
        {
            countLessOrEqual = ~idx;
        }
        return countLessOrEqual / (double)table.Length;
    }

    // Mirror of ExactMethod returning P(U ≥ x) for the discrete distribution. Pre-Tier-2 this
    // was a reverse linear scan; now a single binary search plus a backward tie-walk.
    internal static double ExactComplement(double x, double[] table)
    {
        var idx = Array.BinarySearch(table, x);
        int countGreaterOrEqual;
        if (idx >= 0)
        {
            // Walk backwards to find the first occurrence of x. Everything from there to the
            // end satisfies table[i] ≥ x.
            while (idx > 0 && table[idx - 1] == x) idx--;
            countGreaterOrEqual = table.Length - idx;
        }
        else
        {
            // ~idx is the first index where table[i] > x. Everything from there to the end is
            // strictly greater than x; since there are no equal entries, that equals the
            // count ≥ x.
            countGreaterOrEqual = table.Length - ~idx;
        }
        return countGreaterOrEqual / (double)table.Length;
    }

    internal static int Count(double x, double[] table)
    {
        return table.Count(t => Math.Abs(t - x) < 0.00000000000001);
    }
}