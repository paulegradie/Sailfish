using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;

namespace Sailfish.Analysis.SailDiff.Statistics
{
    /// <summary>
    /// Utilities for multiple-comparisons control and ratio-based effect size confidence intervals.
    /// </summary>
    public static class MultipleComparisons
    {
        // Smallest p-value the comparison helpers report for a genuine difference. A literal 0
        // is indistinguishable downstream from "no comparison computed": it BH-adjusts to q = 0,
        // which the q-value cell labels (SailDiffSignificance.IsSignificantPositive) treat as not
        // significant. So an extreme-but-real difference must never collapse to exactly 0.
        // See LogRatioPValue.
        private const double MinPositivePValue = 1e-300;

        /// <summary>
        /// Apply Benjamini–Hochberg False Discovery Rate control to a set of p-values.
        /// Returns adjusted q-values (FDR) mapped to the same pair keys.
        /// </summary>
        public static Dictionary<(string A, string B), double> BenjaminiHochbergAdjust(
            IDictionary<(string A, string B), double> pValues)
        {
            if (pValues == null || pValues.Count == 0)
                return new();

            // Work on sorted list while keeping original keys
            var items = pValues
                .Select(kv => (Key: NormalizePair(kv.Key.A, kv.Key.B), P: ClampP(kv.Value)))
                .OrderBy(t => t.P)
                .ToArray();

            var qSorted = BenjaminiHochbergAdjustSorted(items.Select(t => t.P).ToArray());

            // Write back using normalized pair keys to ensure (A,B)==(B,A)
            var result = new Dictionary<(string A, string B), double>(pValues.Count);
            for (var i = 0; i < items.Length; i++)
            {
                result[items[i].Key] = qSorted[i];
            }
            return result;
        }

        /// <summary>
        /// String-keyed BH variant for cases where each comparison has a single identifier
        /// (e.g. one SailDiff before/after pair per test-case display name).
        /// </summary>
        public static Dictionary<string, double> BenjaminiHochbergAdjust(IDictionary<string, double> pValues)
        {
            if (pValues == null || pValues.Count == 0)
                return new();

            var items = pValues
                .Select(kv => (Key: kv.Key, P: ClampP(kv.Value)))
                .OrderBy(t => t.P)
                .ToArray();

            var qSorted = BenjaminiHochbergAdjustSorted(items.Select(t => t.P).ToArray());

            var result = new Dictionary<string, double>(pValues.Count);
            for (var i = 0; i < items.Length; i++)
            {
                result[items[i].Key] = qSorted[i];
            }
            return result;
        }

        // Core BH step on a p-value vector that is already sorted ascending. q(i) =
        // min_{j ≥ i} (m/j · p(j)) enforced monotonically from the end so larger p's never
        // produce smaller q's than their predecessors.
        private static double[] BenjaminiHochbergAdjustSorted(double[] sortedAscendingPValues)
        {
            var m = sortedAscendingPValues.Length;
            var q = new double[m];
            var minQ = 1.0;
            for (var i = m - 1; i >= 0; i--)
            {
                var rank = i + 1; // 1-based rank in ascending p
                var bh = (m / (double)rank) * sortedAscendingPValues[i];
                if (bh < minQ) minQ = bh;
                q[i] = Math.Min(1.0, minQ);
            }
            return q;
        }

        /// <summary>
        /// Compute a ratio-based effect size and CI using a log-normal (delta-method) approximation.
        /// ratio = meanB / meanA. CI computed on log scale using SEs, then exponentiated.
        /// If inputs are degenerate (means &lt;= 0 or SEs not available), returns ratio with null CI.
        /// </summary>
        public static (double Ratio, double? Lower, double? Upper) ComputeRatioCi(
            double meanA, double seA, int nA,
            double meanB, double seB, int nB,
            double confidenceLevel = 0.95)
        {
            var ratio = meanB / meanA;
            if (!(meanA > 0) || !(meanB > 0) || (seA <= 0 && seB <= 0))
            {
                return (ratio, null, null);
            }

            // Welch-style conservative df on log scale: use min(nA-1, nB-1) with floor at 1
            var dof = Math.Max(1, Math.Min(Math.Max(0, nA - 1), Math.Max(0, nB - 1)));

            // Delta method on log scale
            var seLog = Math.Sqrt(Square(SafeDiv(seA, meanA)) + Square(SafeDiv(seB, meanB)));
            if (seLog <= 0)
            {
                return (ratio, null, null);
            }

            // Two-tailed critical value
            var t = StudentT.InvCDF(0, 1, dof, 0.5 + confidenceLevel / 2.0);
            var delta = t * seLog;

            var logR = Math.Log(ratio);
            var lower = Math.Exp(logR - delta);
            var upper = Math.Exp(logR + delta);
            return (ratio, lower, upper);
        }

        /// <summary>
        /// Two-sided p-value for H0: meanA == meanB on the log scale, using the same delta-method
        /// standard error and conservative degrees of freedom as <see cref="ComputeRatioCi"/>.
        /// Returns <see cref="double.NaN"/> when the test is undefined — a mean is non-positive, or
        /// neither side has any usable variance (nothing to test against). Otherwise the result is
        /// always strictly positive — see remarks.
        /// </summary>
        /// <remarks>
        /// The tail is evaluated as <c>2·StudentT.CDF(−|t|)</c> — the lower tail, which MathNet
        /// returns directly — rather than the algebraically-equivalent <c>2·(1 − StudentT.CDF(|t|))</c>.
        /// The latter is catastrophic for large <c>t</c>: once the true tail drops below the ULP of
        /// 1.0 (~1.1e-16) the CDF rounds to exactly 1.0, so <c>1 − CDF</c> becomes 0 and the *most
        /// significant* comparisons collapse to <c>p = 0</c>. A 0 is then indistinguishable from
        /// "no comparison computed" and is reported as not significant ("Similar") — the exact
        /// failure where a 400×, perfectly-separated difference was labelled not significant.
        /// Evaluating the lower tail keeps the small-but-positive value (≈1e-18); the floor
        /// guarantees a strictly positive result even past the point where the tail itself
        /// underflows.
        /// </remarks>
        public static double LogRatioPValue(double meanA, double seA, int nA, double meanB, double seB, int nB)
        {
            if (!(meanA > 0) || !(meanB > 0)) return double.NaN;

            var logRatio = Math.Abs(Math.Log(meanB / meanA));
            var seLog = Math.Sqrt(Square(SafeDiv(seA, meanA)) + Square(SafeDiv(seB, meanB)));

            // No usable variance on either side (e.g. N = 1, or StdDev collapsed to 0): there is
            // nothing to run a variance-based test against, so abstain. NaN routes to "not
            // significant" downstream — the deliberate "Similar, no q-value" behaviour for cells
            // whose standard error is unavailable. (The reported zero-variance bug is the *other*
            // case: one side constant, the other with real spread, so seLog > 0 and the tail
            // computation below correctly reports significance.)
            if (seLog <= 0) return double.NaN;

            var t = logRatio / seLog;
            var dof = Math.Max(1, Math.Min(Math.Max(0, nA - 1), Math.Max(0, nB - 1)));
            var p = 2.0 * StudentT.CDF(0, 1, dof, -t);
            return Math.Max(p, MinPositivePValue);
        }

        public static (string A, string B) NormalizePair(string a, string b)
        {
            return string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
        }

        private static double ClampP(double p)
        {
            // NaN p-values arise from degenerate inputs (e.g. Welch's t with zero variance
            // on both sides, or KS on constant samples). Map them to 1.0 — the *most
            // conservative* placement in the BH family — so a single failed test cannot
            // contaminate the right-to-left running-min that drives the q-values for every
            // other pair. The pre-fix mapping `NaN → 0` placed the failed pair at the head
            // of the sorted-ascending vector, dragging unrelated q-values below alpha and
            // fabricating significance. Treating NaN as p=1 keeps the pair in the family
            // (so the key/value correspondence with the input dict is preserved) while
            // ensuring it can never lower another pair's q-value.
            if (double.IsNaN(p)) return 1.0;
            if (p < 0) return 0.0;
            if (p > 1) return 1.0;
            return p;
        }

        private static double SafeDiv(double a, double b) => Math.Abs(b) < double.Epsilon ? 0 : a / b;
        private static double Square(double x) => x * x;
    }
}

