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
        /// If inputs are degenerate (means <= 0 or SEs not available), returns ratio with null CI.
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

        public static (string A, string B) NormalizePair(string a, string b)
        {
            return string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
        }

        private static double ClampP(double p)
        {
            if (double.IsNaN(p) || p < 0) return 0.0;
            if (p > 1) return 1.0;
            return p;
        }

        private static double SafeDiv(double a, double b) => Math.Abs(b) < double.Epsilon ? 0 : a / b;
        private static double Square(double x) => x * x;
    }
}

