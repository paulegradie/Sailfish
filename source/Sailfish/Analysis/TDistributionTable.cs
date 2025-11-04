using System;
using Sailfish.Analysis.SailDiff.Statistics.StatsCore.Distributions;

namespace Sailfish.Analysis
{
    /// <summary>
    /// Provides two-tailed critical values for the Student's t-distribution.
    /// Uses the internal T-distribution implementation to compute quantiles.
    /// </summary>
    public static class TDistributionTable
    {
        /// <summary>
        /// Returns the two-tailed critical t-value for the given confidence level and degrees of freedom.
        /// For confidenceLevel = 0.95, this returns t_{alpha/2, df} with alpha = 0.05.
        /// </summary>
        public static double GetCriticalValue(double confidenceLevel, int degreesOfFreedom)
        {
            if (degreesOfFreedom < 1) degreesOfFreedom = 1;
            if (!(confidenceLevel > 0.0 && confidenceLevel < 1.0)) confidenceLevel = 0.95;

            // Two-tailed critical value: quantile at 1 - alpha/2
            var alpha = 1.0 - confidenceLevel;
            var p = 1.0 - alpha / 2.0;

            try
            {
                var dist = new Distribution(degreesOfFreedom);
                var t = dist.InverseDistributionFunction(p);
                if (!double.IsNaN(t) && !double.IsInfinity(t)) return t;
            }
            catch
            {
                // Fall through to normal approximation
            }

            // Fallback: normal approximation for the requested confidence
            return confidenceLevel switch
            {
                >= 0.999 => 3.291,
                >= 0.99 => 2.576,
                >= 0.95 => 1.960,
                >= 0.90 => 1.645,
                _ => 1.960
            };
        }
    }
}

