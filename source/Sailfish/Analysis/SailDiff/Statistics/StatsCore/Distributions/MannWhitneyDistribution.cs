using System;
using System.Linq;
using System.Threading.Tasks;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Attributes;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Ops;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

public class MannWhitneyDistribution : UnivariateContinuousDistribution
{
    private NormalDistribution approximation;

    public MannWhitneyDistribution([PositiveInteger] int n1, [PositiveInteger] int n2)
    {
        Init(n1, n2, null, new bool?());
    }

    public MannWhitneyDistribution(double[] ranks, [PositiveInteger] int n1, [PositiveInteger] int n2, bool? exact = null)
    {
        Init(n1, n2, ranks, exact);
    }

    public MannWhitneyDistribution(double[] ranks1, double[] ranks2, bool? exact = null)
    {
        var ranks = ranks1.Concatenate(ranks2);
        Init(ranks1.Length, ranks2.Length, ranks, exact);
    }

    public int NumberOfSamples1 { get; private set; }

    public int NumberOfSamples2 { get; private set; }

    public ContinuityCorrection Correction { get; set; }

    public bool Exact { get; private set; }

    public double[] Table { get; private set; }

    public override double Mean => approximation.Mean;

    public override double Variance => approximation.Variance;

    public override double Mode => approximation.Mode;

    public override double Entropy => approximation.Entropy;

    public override DoubleRange Support => new(double.NegativeInfinity, double.PositiveInfinity);

    private void Init(int n1, int n2, double[]? ranks, bool? exact)
    {
        if (n1 <= 0)
            throw new ArgumentOutOfRangeException(nameof(n1), "The number of observations in the first sample (n1) must be higher than zero.");
        if (n2 <= 0)
            throw new ArgumentOutOfRangeException(nameof(n2), "The number of observations in the second sample (n2) must be higher than zero.");
        if (ranks != null)
        {
            if (ranks.Length <= 1)
                throw new ArgumentOutOfRangeException(nameof(ranks), "The rank vector must contain a minimum of 2 elements.");
            for (var index = 0; index < ranks.Length; ++index)
                if (ranks[index] < 0.0)
                    throw new ArgumentOutOfRangeException("The rank values cannot be negative.");
        }

        var num1 = n1 + n2;
        NumberOfSamples1 = n1;
        NumberOfSamples2 = n2;
        var mean = n1 * n2 / 2.0;
        var stdDev = Math.Sqrt(n1 * n2 * (num1 + 1) / 12.0);
        var flag = ranks != null;
        Exact = flag && n1 <= 30 && n2 <= 30;
        if (exact.HasValue)
        {
            if (exact.Value && !flag)
                throw new ArgumentException(nameof(exact), "Cannot use exact method if rank vectors are not specified.");
            Exact = exact.Value;
        }

        if (flag)
        {
            var num2 = Correctionx(ranks);
            stdDev = Math.Sqrt(n1 * n2 / 12.0 * (num1 + 1.0 - num2));
            if (Exact)
                InitExactMethod(ranks);
        }

        approximation = new NormalDistribution(mean, stdDev);
    }

    private static double Correctionx(double[] ranks)
    {
        var length = ranks.Length;
        if (length <= 1)
            throw new ArgumentOutOfRangeException(nameof(ranks));
        var numArray = ranks.Ties();
        var num1 = 0.0;
        for (var index = 0; index < numArray.Length; ++index)
        {
            double num2 = numArray[index] * numArray[index] * numArray[index];
            num1 += num2 - numArray[index];
        }

        return num1 / (length * (length - 1));
    }

    private void InitExactMethod(double[] ranks)
    {
        var k = Math.Min(NumberOfSamples1, NumberOfSamples2);
        var n = (long)Specials.Binomial(NumberOfSamples1 + NumberOfSamples2, k);
        Table = new double[n];
        Parallel.ForEach(ranks.Combinations(k).Zip(Vector.Range(n), (Func<double[], long, Tuple<double[], long>>)((c, i) => new Tuple<double[], long>(c, i))),
            (Action<Tuple<double[], long>>)(i => Table[i.Item2] = MannWhitneyU(i.Item1)));
        Array.Sort(Table);
    }

    protected internal override double InnerDistributionFunction(double x)
    {
        return NumberOfSamples1 <= NumberOfSamples2 ? DistributionFunction(x) : ComplementaryDistributionFunction(x);
    }

    protected internal override double InnerComplementaryDistributionFunction(double x)
    {
        return NumberOfSamples1 <= NumberOfSamples2 ? ComplementaryDistributionFunction(x) : DistributionFunction(x);
    }

    private double DistributionFunction(double x)
    {
        if (Exact)
            return WilcoxonDistribution.ExactMethod(x, Table);
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

    private double ComplementaryDistributionFunction(double x)
    {
        if (Exact)
            return WilcoxonDistribution.ExactComplement(x, Table);
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

    public static double MannWhitneyU(double[] ranks)
    {
        var length = ranks.Length;
        return ranks.Sum() - length * (length + 1.0) / 2.0;
    }

    public override object Clone()
    {
        return new MannWhitneyDistribution(NumberOfSamples1, NumberOfSamples2)
        {
            Exact = Exact,
            Table = Table,
            NumberOfSamples1 = NumberOfSamples1,
            NumberOfSamples2 = NumberOfSamples2,
            Correction = Correction,
            approximation = (NormalDistribution)approximation.Clone()
        };
    }

    protected internal override double InnerProbabilityDensityFunction(double x)
    {
        return Exact ? WilcoxonDistribution.Count(x, Table) / (double)Table.Length : approximation.ProbabilityDensityFunction(x);
    }

    protected internal override double InnerLogProbabilityDensityFunction(double x)
    {
        return Exact ? Math.Log(WilcoxonDistribution.Count(x, Table)) - Math.Log(Table.Length) : approximation.ProbabilityDensityFunction(x);
    }

    protected internal override double InnerInverseDistributionFunction(double p)
    {
        if (!Exact)
            return approximation.InverseDistributionFunction(p);
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

    public override string ToString(string format, IFormatProvider formatProvider)
    {
        var provider = formatProvider;
        var num = NumberOfSamples1;
        var str1 = num.ToString(format, formatProvider);
        num = NumberOfSamples2;
        var str2 = num.ToString(format, formatProvider);
        return string.Format(provider, "MannWhitney(u; n1 = {0}, n2 = {1})", str1, str2);
    }
}