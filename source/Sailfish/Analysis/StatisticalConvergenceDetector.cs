using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;

namespace Sailfish.Analysis;

/// <summary>
/// Implementation of statistical convergence detection using coefficient of variation.
/// This class determines when performance test samples have achieved sufficient statistical stability.
/// </summary>
public class StatisticalConvergenceDetector : IStatisticalConvergenceDetector
{
    /// <summary>
    /// Checks if the provided samples have converged based on coefficient of variation and confidence interval width.
    /// </summary>
    /// <param name="samples">The performance samples to analyze</param>
    /// <param name="targetCoefficientOfVariation">The target CV threshold for convergence</param>
    /// <param name="maxConfidenceIntervalWidth">The maximum acceptable confidence interval width (relative to mean)</param>
    /// <param name="confidenceLevel">The confidence level for statistical analysis</param>
    /// <param name="minimumSampleSize">The minimum number of samples required before checking convergence</param>
    /// <returns>A ConvergenceResult indicating whether convergence has been achieved</returns>
    public ConvergenceResult CheckConvergence(
        IReadOnlyList<double> samples,
        double targetCoefficientOfVariation,
        double maxConfidenceIntervalWidth,
        double confidenceLevel,
        int minimumSampleSize)
    {
        if (samples == null || samples.Count == 0)
        {
            return new ConvergenceResult
            {
                HasConverged = false,
                SampleCount = 0,
                Reason = "No samples provided"
            };
        }

        if (samples.Count < minimumSampleSize)
        {
            return new ConvergenceResult
            {
                HasConverged = false,
                SampleCount = samples.Count,
                Reason = $"Insufficient samples: {samples.Count} < {minimumSampleSize}"
            };
        }

        // Calculate basic statistics
        var samplesArray = samples.ToArray();
        var mean = samplesArray.Mean();
        var standardDeviation = samplesArray.StandardDeviation();
        
        // Handle edge cases
        // Treat very small means as zero to avoid unstable CV and CI calculations
        const double nearZero = 1e-9;
        if (Math.Abs(mean) <= nearZero)
        {
            return new ConvergenceResult
            {
                HasConverged = false,
                CurrentMean = mean,
                CurrentStandardDeviation = standardDeviation,
                SampleCount = samples.Count,
                Reason = "Cannot calculate coefficient of variation: mean is zero"
            };
        }

        if (double.IsNaN(standardDeviation) || double.IsInfinity(standardDeviation))
        {
            return new ConvergenceResult
            {
                HasConverged = false,
                CurrentMean = mean,
                CurrentStandardDeviation = standardDeviation,
                SampleCount = samples.Count,
                Reason = "Invalid standard deviation calculated"
            };
        }

        // Calculate coefficient of variation
        var coefficientOfVariation = Math.Abs(standardDeviation / mean);

        if (double.IsNaN(coefficientOfVariation) || double.IsInfinity(coefficientOfVariation))
        {
            return new ConvergenceResult
            {
                HasConverged = false,
                CurrentMean = mean,
                CurrentStandardDeviation = standardDeviation,
                CurrentCoefficientOfVariation = coefficientOfVariation,
                SampleCount = samples.Count,
                Reason = "Invalid coefficient of variation calculated"
            };
        }

        // Calculate confidence interval
        var standardError = standardDeviation / Math.Sqrt(samples.Count);
        var degreesOfFreedom = samples.Count - 1;
        var tValue = GetTValue(confidenceLevel, degreesOfFreedom);
        var marginOfError = tValue * standardError;
        var ciLower = mean - marginOfError;
        var ciUpper = mean + marginOfError;
        var ciWidth = ciUpper - ciLower;
        var relativeCiWidth = Math.Abs(ciWidth / mean);

        // Check multiple convergence criteria
        var cvConverged = coefficientOfVariation <= targetCoefficientOfVariation;
        var ciConverged = relativeCiWidth <= maxConfidenceIntervalWidth;
        var hasConverged = cvConverged && ciConverged;

        // Generate detailed reason
        var reason = hasConverged
            ? $"Converged: CV {coefficientOfVariation:F4} <= {targetCoefficientOfVariation:F4}, CI width {relativeCiWidth:F4} <= {maxConfidenceIntervalWidth:F4}"
            : $"Not converged: CV {coefficientOfVariation:F4} {(cvConverged ? "<=" : ">")} {targetCoefficientOfVariation:F4}, CI width {relativeCiWidth:F4} {(ciConverged ? "<=" : ">")} {maxConfidenceIntervalWidth:F4}";

        return new ConvergenceResult
        {
            HasConverged = hasConverged,
            CurrentCoefficientOfVariation = coefficientOfVariation,
            CurrentMean = mean,
            CurrentStandardDeviation = standardDeviation,
            StandardError = standardError,
            ConfidenceLevel = confidenceLevel,
            ConfidenceIntervalLower = ciLower,
            ConfidenceIntervalUpper = ciUpper,
            ConfidenceIntervalWidth = ciWidth,
            MarginOfError = marginOfError,
            RelativeConfidenceIntervalWidth = relativeCiWidth,
            SampleCount = samples.Count,
            Reason = reason
        };
    }

    /// <summary>
    /// Checks if the provided samples have converged based on coefficient of variation only (backward compatibility).
    /// </summary>
    /// <param name="samples">The performance samples to analyze</param>
    /// <param name="targetCoefficientOfVariation">The target CV threshold for convergence</param>
    /// <param name="confidenceLevel">The confidence level for statistical analysis</param>
    /// <param name="minimumSampleSize">The minimum number of samples required before checking convergence</param>
    /// <returns>A ConvergenceResult indicating whether convergence has been achieved</returns>
    public ConvergenceResult CheckConvergence(
        IReadOnlyList<double> samples,
        double targetCoefficientOfVariation,
        double confidenceLevel,
        int minimumSampleSize)
    {
        // Use a default relative CI width of 20% for backward compatibility
        return CheckConvergence(samples, targetCoefficientOfVariation, 0.20, confidenceLevel, minimumSampleSize);
    }

    /// <summary>
    /// Gets the critical t-value for the specified confidence level and degrees of freedom.
    /// </summary>
    /// <param name="confidenceLevel">The confidence level (e.g., 0.95 for 95%)</param>
    /// <param name="degreesOfFreedom">The degrees of freedom (sample size - 1)</param>
    /// <returns>The critical t-value</returns>
    private static double GetTValue(double confidenceLevel, int degreesOfFreedom)
    {
        // Delegate to central t-distribution provider for accurate critical values
        return DistributionTable.GetCriticalValue(confidenceLevel, degreesOfFreedom);
    }
}
