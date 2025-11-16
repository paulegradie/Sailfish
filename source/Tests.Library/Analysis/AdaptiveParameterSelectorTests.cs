using Sailfish.Analysis;
using Sailfish.Execution;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis;

public class AdaptiveParameterSelectorTests
{
    private readonly AdaptiveParameterSelector _selector = new();

    [Fact]
    public void Select_WithUltraFastSamples_ReturnsTightBudgets()
    {
        var pilot = new double[] { 10_000, 12_000, 15_000, 9_000, 11_000 }; // ~10-15us
        var settings = new ExecutionSettings { TargetCoefficientOfVariation = 0.02, MaxConfidenceIntervalWidth = 0.25 };

        var config = _selector.Select(pilot, settings);

        config.Category.ShouldBe(AdaptiveSamplingConfig.SpeedCategory.UltraFast);
        config.TargetCoefficientOfVariation.ShouldBeGreaterThanOrEqualTo(0.02); // never tighten beyond requested
        config.MaxConfidenceIntervalWidth.ShouldBeLessThanOrEqualTo(0.25);
        config.RecommendedMinimumSampleSize.ShouldBe(50);
        config.SelectionReason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Select_WithVerySlowSamples_ReturnsLooserBudgets()
    {
        var pilot = new double[] { 120_000_000, 90_000_000, 150_000_000 }; // 90-150ms
        var settings = new ExecutionSettings { TargetCoefficientOfVariation = 0.05, MaxConfidenceIntervalWidth = 0.30 };

        var config = _selector.Select(pilot, settings);

        config.Category.ShouldBe(AdaptiveSamplingConfig.SpeedCategory.VerySlow);
        config.TargetCoefficientOfVariation.ShouldBeGreaterThanOrEqualTo(0.05);
        config.MaxConfidenceIntervalWidth.ShouldBeLessThanOrEqualTo(0.30);
        config.RecommendedMinimumSampleSize.ShouldBe(10);
    }

    [Fact]
    public void Select_WithEmptySamples_FallsBackToSettings()
    {
        var pilot = System.Array.Empty<double>();
        var settings = new ExecutionSettings { TargetCoefficientOfVariation = 0.06, MaxConfidenceIntervalWidth = 0.22 };

        var config = _selector.Select(pilot, settings);

        config.Category.ShouldBe(AdaptiveSamplingConfig.SpeedCategory.Medium);
        config.TargetCoefficientOfVariation.ShouldBe(0.06);
        config.MaxConfidenceIntervalWidth.ShouldBe(0.22);
    }

    [Fact]
    public void Select_WithFastSamples_ShouldRecommendMinN30()
    {
        var pilot = new double[] { 100_000, 200_000, 300_000 }; // 0.1-0.3ms
        var settings = new ExecutionSettings { TargetCoefficientOfVariation = 0.05, MaxConfidenceIntervalWidth = 0.20 };

        var config = _selector.Select(pilot, settings);
        config.Category.ShouldBe(AdaptiveSamplingConfig.SpeedCategory.Fast);
        config.RecommendedMinimumSampleSize.ShouldBe(30);
    }

    [Fact]
    public void Select_WithMediumSamples_ShouldRecommendMinN20()
    {
        var pilot = new double[] { 1_000_000, 2_000_000, 3_000_000 }; // 1-3ms
        var settings = new ExecutionSettings { TargetCoefficientOfVariation = 0.05, MaxConfidenceIntervalWidth = 0.20 };

        var config = _selector.Select(pilot, settings);
        config.Category.ShouldBe(AdaptiveSamplingConfig.SpeedCategory.Medium);
        config.RecommendedMinimumSampleSize.ShouldBe(20);
    }

    [Fact]
    public void Select_WithSlowSamples_ShouldRecommendMinN15()
    {
        var pilot = new double[] { 10_000_000, 12_000_000, 8_000_000 }; // 8-12ms
        var settings = new ExecutionSettings { TargetCoefficientOfVariation = 0.05, MaxConfidenceIntervalWidth = 0.20 };

        var config = _selector.Select(pilot, settings);
        config.Category.ShouldBe(AdaptiveSamplingConfig.SpeedCategory.Slow);
        config.RecommendedMinimumSampleSize.ShouldBe(15);
    }

}

