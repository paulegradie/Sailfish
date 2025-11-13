using System;

namespace Sailfish.Execution;

/// <summary>
/// Suggested adaptive sampling parameters derived from a pilot sample analysis.
/// </summary>
public sealed class AdaptiveSamplingConfig
{
    public enum SpeedCategory
    {
        UltraFast,
        Fast,
        Medium,
        Slow,
        VerySlow
    }

    public AdaptiveSamplingConfig(
        SpeedCategory category,
        double targetCoefficientOfVariation,
        double maxConfidenceIntervalWidth)
    {
        Category = category;
        TargetCoefficientOfVariation = targetCoefficientOfVariation;
        MaxConfidenceIntervalWidth = maxConfidenceIntervalWidth;
        RecommendedMinimumSampleSize = 0;
        SelectionReason = null;
    }

    public AdaptiveSamplingConfig(
        SpeedCategory category,
        double targetCoefficientOfVariation,
        double maxConfidenceIntervalWidth,
        int recommendedMinimumSampleSize,
        string? selectionReason)
    {
        Category = category;
        TargetCoefficientOfVariation = targetCoefficientOfVariation;
        MaxConfidenceIntervalWidth = maxConfidenceIntervalWidth;
        RecommendedMinimumSampleSize = Math.Max(0, recommendedMinimumSampleSize);
        SelectionReason = selectionReason;
    }

    public SpeedCategory Category { get; }

    /// <summary>Recommended CV target to reach for this speed category.</summary>
    public double TargetCoefficientOfVariation { get; }

    /// <summary>Recommended relative CI width budget for this speed category.</summary>
    public double MaxConfidenceIntervalWidth { get; }

    /// <summary>Recommended minimum N for convergence gates (local floor only).</summary>
    public int RecommendedMinimumSampleSize { get; }

    /// <summary>Optional explanation of the selection.</summary>
    public string? SelectionReason { get; }
}

