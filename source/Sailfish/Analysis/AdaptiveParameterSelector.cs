using System;
using System.Collections.Generic;
using MathNet.Numerics.Statistics;
using Sailfish.Execution;

namespace Sailfish.Analysis;

/// <summary>
/// Selects adaptive sampling parameters by classifying a method's speed
/// from a set of pilot samples (nanoseconds).
/// </summary>
public class AdaptiveParameterSelector
{
    /// <summary>
    /// Classify pilot samples and return recommended adaptive sampling configuration.
    /// Does not mutate execution settings; intended to be used to tune local thresholds.
    /// </summary>
    public AdaptiveSamplingConfig Select(IReadOnlyList<double> pilotSamples, IExecutionSettings executionSettings)
    {
        ArgumentNullException.ThrowIfNull(executionSettings);

        if (pilotSamples == null || pilotSamples.Count == 0)
        {
            // Fall back to defaults from executionSettings
            return new AdaptiveSamplingConfig(
                AdaptiveSamplingConfig.SpeedCategory.Medium,
                executionSettings.TargetCoefficientOfVariation,
                executionSettings.MaxConfidenceIntervalWidth);
        }

        // Use robust statistic (median) for speed classification
        var medianNs = pilotSamples.Median();

        // Thresholds (nanoseconds) for speed buckets
        // Tuned conservatively to avoid aggressive parameter shifts
        var category = medianNs switch
        {
            < 50_000 => AdaptiveSamplingConfig.SpeedCategory.UltraFast, // < 50µs
            < 500_000 => AdaptiveSamplingConfig.SpeedCategory.Fast,      // < 0.5ms
            < 5_000_000 => AdaptiveSamplingConfig.SpeedCategory.Medium,  // < 5ms
            < 50_000_000 => AdaptiveSamplingConfig.SpeedCategory.Slow,   // < 50ms
            _ => AdaptiveSamplingConfig.SpeedCategory.VerySlow           // >= 50ms
        };

        // Recommended CV and CI budgets by category (kept modest to preserve perf)
        var (cv, ci) = category switch
        {
            AdaptiveSamplingConfig.SpeedCategory.UltraFast => (0.03, 0.12),
            AdaptiveSamplingConfig.SpeedCategory.Fast => (0.04, 0.15),
            AdaptiveSamplingConfig.SpeedCategory.Medium => (0.05, 0.20),
            AdaptiveSamplingConfig.SpeedCategory.Slow => (0.07, 0.25),
            AdaptiveSamplingConfig.SpeedCategory.VerySlow => (0.10, 0.30),
            _ => (executionSettings.TargetCoefficientOfVariation, executionSettings.MaxConfidenceIntervalWidth)
        };

        // Never tighten beyond what the user asked unless faster; never exceed user's CI budget
        cv = Math.Max(cv, executionSettings.TargetCoefficientOfVariation);
        ci = Math.Min(ci, executionSettings.MaxConfidenceIntervalWidth);

        return new AdaptiveSamplingConfig(category, cv, ci);
    }
}

