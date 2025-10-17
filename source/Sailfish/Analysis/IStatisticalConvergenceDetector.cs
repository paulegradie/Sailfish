using System.Collections.Generic;

namespace Sailfish.Analysis;

/// <summary>
/// Interface for detecting statistical convergence in performance test data.
/// Used by adaptive sampling to determine when sufficient samples have been collected.
/// </summary>
public interface IStatisticalConvergenceDetector
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
    ConvergenceResult CheckConvergence(
        IReadOnlyList<double> samples,
        double targetCoefficientOfVariation,
        double maxConfidenceIntervalWidth,
        double confidenceLevel,
        int minimumSampleSize);
}

/// <summary>
/// Result of a statistical convergence check.
/// Contains information about whether convergence was achieved and current statistical measures including confidence intervals.
/// </summary>
public class ConvergenceResult
{
    /// <summary>
    /// Gets whether the samples have converged to the target threshold.
    /// </summary>
    public bool HasConverged { get; init; }

    /// <summary>
    /// Gets the current coefficient of variation of the samples.
    /// </summary>
    public double CurrentCoefficientOfVariation { get; init; }

    /// <summary>
    /// Gets the current mean of the samples.
    /// </summary>
    public double CurrentMean { get; init; }

    /// <summary>
    /// Gets the current standard deviation of the samples.
    /// </summary>
    public double CurrentStandardDeviation { get; init; }

    /// <summary>
    /// Gets the standard error of the mean.
    /// </summary>
    public double StandardError { get; init; }

    /// <summary>
    /// Gets the confidence level used for interval calculations (e.g., 0.95 for 95% CI).
    /// </summary>
    public double ConfidenceLevel { get; init; } = 0.95;

    /// <summary>
    /// Gets the lower bound of the confidence interval.
    /// </summary>
    public double ConfidenceIntervalLower { get; init; }

    /// <summary>
    /// Gets the upper bound of the confidence interval.
    /// </summary>
    public double ConfidenceIntervalUpper { get; init; }

    /// <summary>
    /// Gets the width of the confidence interval (upper - lower).
    /// </summary>
    public double ConfidenceIntervalWidth { get; init; }

    /// <summary>
    /// Gets the margin of error (half-width of confidence interval).
    /// </summary>
    public double MarginOfError { get; init; }

    /// <summary>
    /// Gets the relative confidence interval width (width / mean).
    /// </summary>
    public double RelativeConfidenceIntervalWidth { get; init; }

    /// <summary>
    /// Gets the number of samples analyzed.
    /// </summary>
    public int SampleCount { get; init; }

    /// <summary>
    /// Gets a human-readable reason for the convergence result.
    /// </summary>
    public string Reason { get; init; } = string.Empty;
}
