using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionBase;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions.DistributionFactories;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.MathOps;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

/// <summary>
/// Null distribution of the Wilcoxon signed-rank statistic <c>W+</c>.
/// </summary>
/// <remarks>
/// <para>
/// Uses the exact DP-derived PMF when N ≤ <see cref="ExactMaxN"/> and the rank vector is
/// untied; otherwise falls back to the normal approximation with tie correction and
/// continuity correction. Pre-Tier-2 the exact path enumerated every one of the <c>2^N</c>
/// sign combinations via <see cref="MathOps.Combinatorics.Sequences(int, bool)"/>, built a
/// flat <c>2^N</c> Table, and sorted it — the same combinatorial-blow-up pattern that hit
/// Mann-Whitney. The DP is <c>O(N³)</c> time and <c>O(N²)</c> space.
/// </para>
/// <para>
/// <strong>Discrete CDF convention.</strong> Both
/// <c>DistributionFunction</c> and <c>ComplementaryDistributionFunction</c>
/// include the queried point: <c>F(w) = P(W+ ≤ w)</c> and <c>F_c(w) = P(W+ ≥ w)</c>.
/// Together they sum to <c>1 + P(W+ = w)</c>, not 1 — the same convention used by
/// <see cref="MannWhitneyDistribution"/> so the signed-rank wrapper's two-tailed p-value
/// formula <c>2·min(F, F_c)</c> stays valid.
/// </para>
/// </remarks>
internal sealed class WilcoxonDistribution : UnivariateContinuousDistribution
{
    /// <summary>
    /// Maximum N for which the exact DP is used. At N=50 the temporary DP table is ~10 KB
    /// and the construction completes in microseconds; beyond that the normal approximation
    /// is accurate to far more digits than benchmark-significance tests consume.
    /// </summary>
    internal const int ExactMaxN = 50;

    private readonly NormalDistribution _approximation;
    private readonly double[]? _pmf;
    private readonly double[]? _cdf;
    private readonly double[]? _ccdf;

    internal WilcoxonDistribution(double[] ranks, bool exact)
    {
        Correction = ContinuityCorrection.Midpoint;

        // Filter zero ranks (paired-difference of zero contributes nothing to W+).
        ranks = ranks.Get(ranks.Find(x => x != 0.0));
        var n = ranks.Length;

        var mean = n * (n + 1.0) / 4.0;
        var variance = n * (n + 1.0) * (2.0 * n + 1.0) / 24.0;
        // Tie correction on the variance (Conover §5.7): subtract (Σ t_i³ − Σ t_i) / 48
        // where t_i are the tie-group sizes.
        var tieAdjustment = 0.0;
        foreach (var groupSize in ranks.Ties())
        {
            if (groupSize <= 1) continue;
            tieAdjustment += (double)groupSize * groupSize * groupSize - groupSize;
        }
        variance -= tieAdjustment / 48.0;
        if (variance < 0) variance = 0;
        var stdDev = Math.Sqrt(variance);
        _approximation = NormalDistributionFactory.Create(mean, stdDev);

        // Honour the caller's request, but only run the exact DP when it's actually safe:
        // small N and no ties (the DP assumes the rank vector is the integer permutation
        // 1..N, which is exactly the untied case).
        Exact = exact && n > 0 && n <= ExactMaxN && !HasTies(ranks);
        if (!Exact) return;

        _pmf = WilcoxonSignedRankExactCdf.Pmf(n);
        _cdf = new double[_pmf.Length];
        _ccdf = new double[_pmf.Length];

        var cumulative = 0.0;
        for (var w = 0; w < _pmf.Length; w++)
        {
            cumulative += _pmf[w];
            _cdf[w] = cumulative;
        }
        for (var w = 0; w < _pmf.Length; w++)
            _ccdf[w] = 1.0 - _cdf[w] + _pmf[w];

        // Pin the endpoints to defeat accumulated floating-point drift.
        _cdf[_pmf.Length - 1] = 1.0;
        _ccdf[0] = 1.0;
    }

    public bool Exact { get; }

    public ContinuityCorrection Correction { get; set; }

    public override double Mean => _approximation.Mean;

    public override DoubleRange Support => Exact ? new DoubleRange(0.0, double.PositiveInfinity) : _approximation.Support;

    private static bool HasTies(double[] ranks)
    {
        foreach (var groupSize in ranks.Ties())
            if (groupSize > 1) return true;
        return false;
    }

    /// <summary>
    /// Sum of the ranks whose sign in <paramref name="signs"/> is positive — the W+
    /// statistic for a particular sign assignment.
    /// </summary>
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
        {
            if (x < 0) return 0.0;
            if (x > _cdf!.Length - 1) return 1.0;
            var idx = (int)Math.Floor(x);
            if (idx < 0) return 0.0;
            if (idx >= _cdf.Length) return 1.0;
            return _cdf[idx];
        }

        if (x > Mean) x -= 0.5;
        else x += 0.5;
        return _approximation.DistributionFunction(x);
    }

    protected override double InnerComplementaryDistributionFunction(double x)
    {
        if (Exact)
        {
            if (x <= 0) return 1.0;
            if (x > _ccdf!.Length - 1) return 0.0;
            var idx = (int)Math.Ceiling(x);
            if (idx < 0) return 1.0;
            if (idx >= _ccdf.Length) return 0.0;
            return _ccdf[idx];
        }

        if (x > Mean) x -= 0.5;
        else x += 0.5;
        return _approximation.ComplementaryDistributionFunction(x);
    }

    protected override double InnerInverseDistributionFunction(double p)
    {
        return Exact ? base.InnerInverseDistributionFunction(p) : _approximation.InverseDistributionFunction(p);
    }

    protected override double InnerProbabilityDensityFunction(double x)
    {
        if (Exact)
        {
            if (x < 0 || x > _pmf!.Length - 1) return 0.0;
            var idx = (int)Math.Round(x);
            if (idx < 0 || idx >= _pmf.Length) return 0.0;
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

    public override string ToString(string? format, IFormatProvider? formatProvider)
    {
        return string.Format(formatProvider, "W+(x; R)");
    }
}
