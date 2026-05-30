using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionFactories;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Search;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

/// <summary>
/// Null distribution of the Mann-Whitney / Wilcoxon rank-sum U statistic.
/// </summary>
/// <remarks>
/// <para>
/// Uses the exact DP-derived PMF when both samples have <c>N ≤ 50</c> and the rank vector
/// is untied; otherwise falls back to the normal approximation with tie correction and
/// continuity correction. Pre-Tier-2, the exact path materialised every combination via
/// <see cref="MathOps.Combinatorics.Combinations{T}"/> and a <c>Vector.Range</c>-backed
/// <c>IEnumerable</c>; at N≈11 that was ~20 MB of garbage <em>per</em> distribution
/// construction. The DP brings that down to ~60 KB.
/// </para>
/// <para>
/// <strong>Discrete CDF convention.</strong> Because U is integer-valued, both
/// <see cref="DistributionFunction"/> and <see cref="ComplementaryDistributionFunction"/>
/// include the queried point: <c>F(u) = P(U ≤ u)</c> and <c>F_c(u) = P(U ≥ u)</c>. Together
/// they sum to <c>1 + P(U = u)</c>, not 1. The wrapper uses this asymmetry to compute the
/// two-tailed p-value as <c>2·min(F(u), F_c(u))</c> — the correct discrete formula.
/// </para>
/// </remarks>
internal sealed class MannWhitneyDistribution : UnivariateContinuousDistribution
{
    /// <summary>
    /// Per-side maximum for which the exact DP is used. At <c>n1 = n2 = 50</c> the temporary
    /// DP table is ~1 MB and runs in microseconds; beyond that the normal approximation is
    /// accurate to far more digits than benchmark-significance tests ever consume.
    /// </summary>
    internal const int ExactMaxN = 50;

    private readonly NormalDistribution _approximation;
    private readonly double[]? _pmf;
    private readonly double[]? _cdf;
    private readonly double[]? _ccdf;

    internal MannWhitneyDistribution(double[] ranks, int rank1Length, int rank2Length)
    {
        NumberOfSamples1 = rank1Length;
        NumberOfSamples2 = rank2Length;

        var combined = rank1Length + rank2Length;
        var mean = rank1Length * rank2Length / 2.0;
        var tieCorrection = Corrections(ranks);
        var stdDev = Math.Sqrt(rank1Length * rank2Length / 12.0 * (combined + 1.0 - tieCorrection));
        _approximation = NormalDistributionFactory.Create(mean, stdDev);
        Correction = ContinuityCorrection.Midpoint;

        // Eligibility for the exact path: both samples within the DP cap, AND the ranks are
        // untied (the DP assumes the rank vector is a permutation of integers). Tied rank
        // vectors carry fractional values (e.g. 3.5) and route through the normal
        // approximation, whose tie-corrected variance — wired above — gives an accurate
        // p-value down to N≈8 per side (Conover, §5.1).
        Exact = rank1Length > 0
                && rank2Length > 0
                && rank1Length <= ExactMaxN
                && rank2Length <= ExactMaxN
                && !HasFractionalRanks(ranks);

        if (!Exact) return;

        _pmf = MannWhitneyExactCdf.Pmf(rank1Length, rank2Length);
        _cdf = new double[_pmf.Length];
        _ccdf = new double[_pmf.Length];

        var cumulative = 0.0;
        for (var u = 0; u < _pmf.Length; u++)
        {
            cumulative += _pmf[u];
            _cdf[u] = cumulative;
        }

        // _ccdf[u] = P(U ≥ u) = 1 - P(U < u) = 1 - (cdf[u] - pmf[u]). Subtract in this order
        // to keep cumulative rounding error from drifting the tails away from 0/1.
        for (var u = 0; u < _pmf.Length; u++)
            _ccdf[u] = 1.0 - _cdf[u] + _pmf[u];

        // Clamp tiny negative values that can appear from floating-point cancellation at the
        // ends of the support.
        _cdf[_pmf.Length - 1] = 1.0;
        _ccdf[0] = 1.0;
    }

    public int NumberOfSamples1 { get; }

    public int NumberOfSamples2 { get; }

    public ContinuityCorrection Correction { get; init; }

    public bool Exact { get; }

    public override double Mean => _approximation.Mean;

    public override DoubleRange Support => new(double.NegativeInfinity, double.PositiveInfinity);

    private static bool HasFractionalRanks(double[] ranks)
    {
        // Untied rank vectors are permutations of 1..N (integer-valued). Any tie-broken rank
        // vector contains at least one half-integer (3.5 etc.). A single sweep is plenty —
        // the rank vector is small relative to anything else we'd allocate.
        for (var i = 0; i < ranks.Length; i++)
            if (ranks[i] != Math.Floor(ranks[i]))
                return true;
        return false;
    }

    private static double Corrections(double[] ranks)
    {
        var length = ranks.Length;
        if (length <= 1)
            throw new ArgumentOutOfRangeException(nameof(ranks));
        var numArray = ranks.Ties();
        var num1 = 0.0;
        foreach (var t in numArray)
        {
            var cubed = (double)t * t * t;
            num1 += cubed - t;
        }

        return num1 / (length * (length - 1));
    }

    protected override double InnerComplementaryDistributionFunction(double x)
    {
        return NumberOfSamples1 <= NumberOfSamples2 ? ComplementaryDistributionFunction(x) : DistributionFunction(x);
    }

    public override double DistributionFunction(double x)
    {
        if (double.IsNaN(x))
            throw new ArgumentOutOfRangeException(nameof(x), "Value cannot be NaN.");

        if (Exact)
        {
            // CDF lookup for the discrete distribution: P(U ≤ floor(x)) over the support
            // {0, ..., n1·n2}. Out-of-support queries saturate to 0 or 1.
            if (x < 0) return 0.0;
            if (x >= _cdf!.Length - 1) return 1.0;
            var idx = (int)Math.Floor(x);
            return _cdf[idx];
        }

        if (x > Mean)
            x -= 0.5;
        else
            x += 0.5;

        return _approximation.DistributionFunction(x);
    }

    private new double ComplementaryDistributionFunction(double x)
    {
        if (Exact)
        {
            // P(U ≥ ceil(x)) for the discrete distribution.
            if (x <= 0) return 1.0;
            if (x > _ccdf!.Length - 1) return 0.0;
            var idx = (int)Math.Ceiling(x);
            return _ccdf[idx];
        }

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

        return _approximation.ComplementaryDistributionFunction(x);
    }

    protected override double InnerProbabilityDensityFunction(double x)
    {
        if (Exact)
        {
            // PMF is non-zero only at integer points in the support.
            if (x < 0 || x >= _pmf!.Length) return 0.0;
            var idx = (int)Math.Round(x);
            return Math.Abs(x - idx) > 1e-9 ? 0.0 : _pmf[idx];
        }
        return _approximation.ProbabilityDensityFunction(x);
    }

    protected override double InnerLogProbabilityDensityFunction(double x)
    {
        if (Exact)
        {
            var pmf = InnerProbabilityDensityFunction(x);
            return pmf <= 0 ? double.NegativeInfinity : Math.Log(pmf);
        }
        return _approximation.LogProbabilityDensityFunction(x);
    }

    protected override double InnerInverseDistributionFunction(double p)
    {
        if (!Exact) return _approximation.InverseDistributionFunction(p);
        if (NumberOfSamples1 <= NumberOfSamples2)
            return base.InnerInverseDistributionFunction(p);

        // Reverse-asymmetric branch preserved from the legacy implementation for callers that
        // request the ICDF with n1 > n2: search outward from 0 for the bounding U values, then
        // refine with Brent. This path remains for API compatibility; in practice the wrapper
        // never calls ICDF.
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
        var str1 = NumberOfSamples1.ToString(format, formatProvider);
        var str2 = NumberOfSamples2.ToString(format, formatProvider);
        return string.Format(formatProvider, "MannWhitney(u; n1 = {0}, n2 = {1})", str1, str2);
    }
}
