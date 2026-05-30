using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

/// <summary>
/// Exact null distribution of the Wilcoxon signed-rank statistic <c>W+</c> for <c>N</c>
/// paired observations with <strong>no tied ranks</strong>.
/// </summary>
/// <remarks>
/// <para>
/// Standard subset-sum DP: <c>f(n, w) = f(n-1, w) + f(n-1, w-n)</c> — each rank either
/// contributes to the positive-signed sum or doesn't. Under the null hypothesis each of
/// the <c>2^n</c> sign combinations is equally likely, so <c>P(W+ = w) = f(n, w) / 2^n</c>.
/// </para>
/// <para>
/// Time: <c>O(n · w_max)</c> ≈ <c>O(n³)</c>. Space: <c>O(w_max)</c> ≈ <c>O(n²)</c>. For
/// <c>n = 50</c> the table is ~10 KB. Pre-Tier-2 the equivalent used a flat <c>2^n</c>
/// lookup table populated by enumerating every sign combination via
/// <see cref="MathOps.Combinatorics.Sequences(int, bool)"/> — same combinatorial blow-up
/// pattern Mann-Whitney suffered.
/// </para>
/// </remarks>
internal static class WilcoxonSignedRankExactCdf
{
    /// <summary>
    /// Returns <c>pmf[w]</c> for <c>w ∈ {0, …, n(n+1)/2}</c> under the untied null.
    /// </summary>
    public static double[] Pmf(int n)
    {
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), "Sample size must be non-negative.");

        if (n == 0) return [1.0];

        var maxW = n * (n + 1) / 2;
        var supportSize = maxW + 1;

        // counts[w] = number of subsets of {1..i} that sum to w, rolled on i from 1..n.
        // Iterating w from high to low lets us update in place: counts[w] += counts[w - i]
        // never touches a cell that we still need for the same iteration.
        var counts = new double[supportSize];
        counts[0] = 1.0;

        for (var i = 1; i <= n; i++)
        {
            for (var w = supportSize - 1; w >= i; w--)
                counts[w] += counts[w - i];
        }

        // Normalise to a PMF. Total subsets = 2^n; we divide by that rather than recomputing
        // the sum to avoid losing precision on the cancellation when n is large.
        var total = 1.0;
        for (var i = 0; i < n; i++) total *= 2.0;
        for (var w = 0; w < supportSize; w++) counts[w] /= total;
        return counts;
    }
}
