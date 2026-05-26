using Sailfish;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Shouldly;
using Xunit;

namespace Tests.Library;

public class RunSettingsBuilderPresetTests
{
    [Fact]
    public void WithPreset_ReturnsSameBuilder()
    {
        var builder = RunSettingsBuilder.CreateBuilder();

        var result = builder.WithPreset(SailfishPreset.Default);

        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void WithPreset_Default_PopulatesAllGlobalSettings()
    {
        var settings = RunSettingsBuilder.CreateBuilder()
            .WithPreset(SailfishPreset.Default)
            .Build();

        settings.GlobalUseAdaptiveSampling.ShouldBe(true);
        settings.GlobalTargetCoefficientOfVariation.ShouldBe(0.05);
        settings.GlobalMaxConfidenceIntervalWidth.ShouldBe(0.20);
        settings.GlobalMinimumSampleSize.ShouldBe(10);
        settings.GlobalMaximumSampleSize.ShouldBe(1000);
        settings.GlobalUseConfigurableOutlierDetection.ShouldBe(true);
        settings.GlobalOutlierStrategy.ShouldBe(OutlierStrategy.RemoveUpper);
        settings.SailDiffSettings.Alpha.ShouldBe(0.001);
        settings.SailDiffSettings.TestType.ShouldBe(TestType.TwoSampleWilcoxonSignedRankTest);
    }

    [Fact]
    public void WithPreset_Tight_PopulatesAllGlobalSettings()
    {
        var settings = RunSettingsBuilder.CreateBuilder()
            .WithPreset(SailfishPreset.Tight)
            .Build();

        settings.GlobalUseAdaptiveSampling.ShouldBe(true);
        settings.GlobalTargetCoefficientOfVariation.ShouldBe(0.03);
        settings.GlobalMaxConfidenceIntervalWidth.ShouldBe(0.12);
        settings.GlobalMinimumSampleSize.ShouldBe(50);
        settings.GlobalMaximumSampleSize.ShouldBe(2000);
        settings.GlobalUseConfigurableOutlierDetection.ShouldBe(true);
        settings.GlobalOutlierStrategy.ShouldBe(OutlierStrategy.RemoveUpper);
        settings.SailDiffSettings.Alpha.ShouldBe(0.0005);
        settings.SailDiffSettings.TestType.ShouldBe(TestType.TwoSampleWilcoxonSignedRankTest);
    }

    [Fact]
    public void WithPreset_Relaxed_PopulatesAllGlobalSettings()
    {
        var settings = RunSettingsBuilder.CreateBuilder()
            .WithPreset(SailfishPreset.Relaxed)
            .Build();

        settings.GlobalUseAdaptiveSampling.ShouldBe(true);
        settings.GlobalTargetCoefficientOfVariation.ShouldBe(0.10);
        settings.GlobalMaxConfidenceIntervalWidth.ShouldBe(0.30);
        settings.GlobalMinimumSampleSize.ShouldBe(10);
        settings.GlobalMaximumSampleSize.ShouldBe(1000);
        settings.GlobalUseConfigurableOutlierDetection.ShouldBe(true);
        settings.GlobalOutlierStrategy.ShouldBe(OutlierStrategy.Adaptive);
        settings.SailDiffSettings.Alpha.ShouldBe(0.01);
        settings.SailDiffSettings.TestType.ShouldBe(TestType.TwoSampleWilcoxonSignedRankTest);
    }

    [Fact]
    public void WithPreset_DoesNotEnableSailDiff()
    {
        var settings = RunSettingsBuilder.CreateBuilder()
            .WithPreset(SailfishPreset.Default)
            .Build();

        settings.RunSailDiff.ShouldBeFalse();
    }

    [Fact]
    public void WithPreset_Then_WithGlobalAdaptiveSampling_ExplicitCallWins()
    {
        var settings = RunSettingsBuilder.CreateBuilder()
            .WithPreset(SailfishPreset.Tight)
            .WithGlobalAdaptiveSampling(targetCoefficientOfVariation: 0.07, maximumSampleSize: 500)
            .Build();

        settings.GlobalTargetCoefficientOfVariation.ShouldBe(0.07);
        settings.GlobalMaximumSampleSize.ShouldBe(500);
        // Fields not touched by WithGlobalAdaptiveSampling keep preset values.
        settings.GlobalMaxConfidenceIntervalWidth.ShouldBe(0.12);
        settings.GlobalMinimumSampleSize.ShouldBe(50);
    }

    [Fact]
    public void WithGlobalAdaptiveSampling_Then_WithPreset_ExplicitCallWins()
    {
        var settings = RunSettingsBuilder.CreateBuilder()
            .WithGlobalAdaptiveSampling(targetCoefficientOfVariation: 0.07, maximumSampleSize: 500)
            .WithPreset(SailfishPreset.Tight)
            .Build();

        settings.GlobalTargetCoefficientOfVariation.ShouldBe(0.07);
        settings.GlobalMaximumSampleSize.ShouldBe(500);
        // Fields not touched by WithGlobalAdaptiveSampling pick up preset values.
        settings.GlobalMaxConfidenceIntervalWidth.ShouldBe(0.12);
        settings.GlobalMinimumSampleSize.ShouldBe(50);
    }

    [Fact]
    public void WithGlobalOutlierHandling_Before_WithPreset_ExplicitCallWins()
    {
        var settings = RunSettingsBuilder.CreateBuilder()
            .WithGlobalOutlierHandling(useConfigurable: true, OutlierStrategy.DontRemove)
            .WithPreset(SailfishPreset.Default)
            .Build();

        settings.GlobalOutlierStrategy.ShouldBe(OutlierStrategy.DontRemove);
    }

    [Fact]
    public void WithSailDiff_Before_WithPreset_CustomSailDiffSurvives()
    {
        var custom = new SailDiffSettings(alpha: 0.05, round: 7);

        var settings = RunSettingsBuilder.CreateBuilder()
            .WithSailDiff(custom)
            .WithPreset(SailfishPreset.Default)
            .Build();

        settings.SailDiffSettings.ShouldBeSameAs(custom);
        settings.SailDiffSettings.Alpha.ShouldBe(0.05);
    }

    [Fact]
    public void WithPreset_CalledTwice_LastCallWins()
    {
        // Regression for codex review #r2787896254: previously the first preset stuck
        // because ApplyPreset eagerly used ??=. Now preset is applied at Build() so
        // a later WithPreset call cleanly replaces an earlier one for BOTH execution
        // settings and SailDiff settings — no silent divergence.
        var settings = RunSettingsBuilder.CreateBuilder()
            .WithPreset(SailfishPreset.Default)
            .WithPreset(SailfishPreset.Relaxed)
            .Build();

        settings.GlobalTargetCoefficientOfVariation.ShouldBe(0.10);
        settings.GlobalMaxConfidenceIntervalWidth.ShouldBe(0.30);
        settings.GlobalOutlierStrategy.ShouldBe(OutlierStrategy.Adaptive);
        settings.SailDiffSettings.Alpha.ShouldBe(0.01);
    }

    [Fact]
    public void SailDiffSettings_PresetConstructor_Default_SetsAlpha()
    {
        var s = new SailDiffSettings(SailfishPreset.Default);
        s.Alpha.ShouldBe(0.001);
        s.TestType.ShouldBe(TestType.TwoSampleWilcoxonSignedRankTest);
    }

    [Fact]
    public void SailDiffSettings_PresetConstructor_Tight_SetsAlpha()
    {
        var s = new SailDiffSettings(SailfishPreset.Tight);
        s.Alpha.ShouldBe(0.0005);
        s.TestType.ShouldBe(TestType.TwoSampleWilcoxonSignedRankTest);
    }

    [Fact]
    public void SailDiffSettings_PresetConstructor_Relaxed_SetsAlpha()
    {
        var s = new SailDiffSettings(SailfishPreset.Relaxed);
        s.Alpha.ShouldBe(0.01);
        s.TestType.ShouldBe(TestType.TwoSampleWilcoxonSignedRankTest);
    }
}
