using Sailfish.Analysis;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis;

public class AdaptiveParameterSelectorTests
{
    private readonly AdaptiveParameterSelector selector = new();

    [Fact]
    public void Select_WithUltraFastSamples_ReturnsTightBudgets()
    {
        var pilot = new double[] { 10_000, 12_000, 15_000, 9_000, 11_000 }; // ~10-15us
        var settings = new ExecutionSettings { TargetCoefficientOfVariation = 0.02, MaxConfidenceIntervalWidth = 0.25 };

        var config = selector.Select(pilot, settings);

        config.Category.ShouldBe(AdaptiveSamplingConfig.SpeedCategory.UltraFast);
        config.TargetCoefficientOfVariation.ShouldBeGreaterThanOrEqualTo(0.02); // never tighten beyond requested
        config.MaxConfidenceIntervalWidth.ShouldBeLessThanOrEqualTo(0.25);
    }

    [Fact]
    public void Select_WithVerySlowSamples_ReturnsLooserBudgets()
    {
        var pilot = new double[] { 120_000_000, 90_000_000, 150_000_000 }; // 90-150ms
        var settings = new ExecutionSettings { TargetCoefficientOfVariation = 0.05, MaxConfidenceIntervalWidth = 0.30 };

        var config = selector.Select(pilot, settings);

        config.Category.ShouldBe(AdaptiveSamplingConfig.SpeedCategory.VerySlow);
        config.TargetCoefficientOfVariation.ShouldBeGreaterThanOrEqualTo(0.05);
        config.MaxConfidenceIntervalWidth.ShouldBeLessThanOrEqualTo(0.30);
    }

    [Fact]
    public void Select_WithEmptySamples_FallsBackToSettings()
    {
        var pilot = System.Array.Empty<double>();
        var settings = new ExecutionSettings { TargetCoefficientOfVariation = 0.06, MaxConfidenceIntervalWidth = 0.22 };

        var config = selector.Select(pilot, settings);

        config.Category.ShouldBe(AdaptiveSamplingConfig.SpeedCategory.Medium);
        config.TargetCoefficientOfVariation.ShouldBe(0.06);
        config.MaxConfidenceIntervalWidth.ShouldBe(0.22);
    }
}

