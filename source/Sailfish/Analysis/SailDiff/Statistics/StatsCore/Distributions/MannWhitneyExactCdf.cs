using System;

namespace Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

/// <summary>
/// Computes the exact null distribution of the Mann-Whitney U statistic for two independent
/// samples of sizes <c>n1</c> and <c>n2</c> with <strong>no tied ranks</strong>.
/// </summary>
/// <remarks>
/// <para>
/// Uses the textbook dynamic-programming recurrence
/// <c>f(n, m, u) = f(n-1, m, u-m) + f(n, m-1, u)</c>
/// (see Hollander, Wolfe &amp; Chicken, <em>Nonparametric Statistical Methods</em>, 3rd ed.,
/// §4.1; or Conover, <em>Practical Nonparametric Statistics</em>, 3rd ed., §5.1). The result
/// is the count of rank arrangements with U-statistic equal to each value in the support
/// <c>{0, …, n1·n2}</c>; dividing by the total <c>C(n1+n2, n1)</c> gives the exact PMF.
/// </para>
/// <para>
/// This replaces the previous combinatorial enumeration in <see cref="MannWhitneyDistribution"/>
/// which materialised every one of the <c>C(n1+n2, n1)</c> arrangements as a cloned
/// <c>double[k]</c> — quadratic memory in N and the source of pre-Tier-2 OOMs at small N.
/// The DP runs in <c>O(n1 · n2² · m)</c> time and <c>O(n1 · n2)</c> peak memory.
/// </para>
/// <para>
/// The implementation assumes untied integer ranks. Callers must detect ties up-front and
/// route tied inputs to the normal approximation with tie correction.
/// </para>
/// </remarks>
internal static class MannWhitneyExactCdf
{
    /// <summary>
    /// Returns <c>pmf[u] = P(U = u)</c> for <c>u ∈ {0, …, n1·n2}</c> under the untied null.
    /// </summary>
    public static double[] Pmf(int n1, int n2)
    {
        if (n1 < 0) throw new ArgumentOutOfRangeException(nameof(n1), "Sample size must be non-negative.");
        if (n2 < 0) throw new ArgumentOutOfRangeException(nameof(n2), "Sample size must be non-negative.");

        // Degenerate edge cases: an empty sample collapses the support to {0}.
        if (n1 == 0 || n2 == 0) return [1.0];

        var maxU = n1 * n2;
        var supportSize = maxU + 1;

        // Two layers of f(n, m, u): one for the previous `n` and one being built. Each layer
        // is a (n2+1) × (maxU+1) grid; we keep two and swap. At n1=n2=50 that's 51 × 2501 ×
        // 8 bytes × 2 ≈ 2 MB — capped well below the previous 16 MB Table allocation.
        var prev = new double[n2 + 1, supportSize];
        var curr = new double[n2 + 1, supportSize];

        // Base case: f(0, m, u) = 1 iff u == 0. (Zero sample-1 elements → only U = 0 is reachable.)
        for (var m = 0; m <= n2; m++) prev[m, 0] = 1.0;

        for (var n = 1; n <= n1; n++)
        {
            Array.Clear(curr);

            // Boundary case along m: f(n, 0, u) = f(n-1, 0, u) (no sample-2 elements; recurrence
            // reduces to copying down `n`). Inductively f(n, 0, u) = 1 iff u == 0 for all n.
            curr[0, 0] = 1.0;

            for (var m = 1; m <= n2; m++)
            {
                // For the current (n, m) layer the support of U extends only to n·m; cells
                // beyond that stay at 0 from the Array.Clear above.
                var upperU = n * m;
                for (var u = 0; u <= upperU; u++)
                {
                    var fromPreviousN = u >= m ? prev[m, u - m] : 0.0;
                    var fromSameLayer = curr[m - 1, u];
                    curr[m, u] = fromPreviousN + fromSameLayer;
                }
            }

            (prev, curr) = (curr, prev);
        }

        // Counts at the corner (n1, n2). Total = C(n1+n2, n1).
        var counts = new double[supportSize];
        var total = 0.0;
        for (var u = 0; u < supportSize; u++)
        {
            counts[u] = prev[n2, u];
            total += counts[u];
        }

        // Normalise to a PMF. If total is somehow zero (shouldn't happen for n1,n2 ≥ 1), bail
        // out with a uniform-mass at u=0 rather than producing NaNs.
        if (total <= 0)
        {
            counts[0] = 1.0;
            return counts;
        }

        for (var u = 0; u < supportSize; u++) counts[u] /= total;
        return counts;
    }
}
