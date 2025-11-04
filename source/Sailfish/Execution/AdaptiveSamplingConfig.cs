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
    }

    public SpeedCategory Category { get; }

    /// <summary>Recommended CV target to reach for this speed category.</summary>
    public double TargetCoefficientOfVariation { get; }

    /// <summary>Recommended relative CI width budget for this speed category.</summary>
    public double MaxConfidenceIntervalWidth { get; }
}

