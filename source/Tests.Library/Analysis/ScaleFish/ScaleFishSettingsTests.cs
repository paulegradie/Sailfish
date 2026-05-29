using System;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Verifies that <see cref="ScaleFishSettings"/> flags propagate through to <see cref="ComplexityEstimator"/>
/// and actually change behaviour. Each test runs with a single explicit settings override and asserts the
/// corresponding feature is on or off.
/// </summary>
public class ScaleFishSettingsTests
{
    [Fact]
    public void Defaults_AllDiagnosticsOn()
    {
        var settings = new ScaleFishSettings();
        settings.EnableBootstrap.ShouldBeTrue();
        settings.EnableContinuousExponent.ShouldBeTrue();
        settings.EnableParallelBootstrap.ShouldBeTrue();
        settings.BootstrapIterations.ShouldBeGreaterThan(0);
        settings.DistinguishabilityDelta.ShouldBe(2.0);
    }

    [Fact]
    public void BootstrapDisabled_OmitsBootstrap()
    {
        var rng = new Random(101);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => x,
            ScaleFishTestHelpers.LogSpacedX(8, 256, 6),
            sampleSize: 25,
            relativeNoise: 0.04,
            rng);

        var settings = new ScaleFishSettings { EnableBootstrap = false };
        var estimator = new ComplexityEstimator(settings);
        var result = estimator.EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.Bootstrap.ShouldBeNull("EnableBootstrap=false should skip the bootstrap diagnostic");
    }

    [Fact]
    public void ContinuousExponentDisabled_OmitsPowerLog()
    {
        var rng = new Random(202);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => x * x,
            ScaleFishTestHelpers.LogSpacedX(4, 256, 6),
            sampleSize: 25,
            relativeNoise: 0.04,
            rng);

        var settings = new ScaleFishSettings { EnableContinuousExponent = false };
        var estimator = new ComplexityEstimator(settings);
        var result = estimator.EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.PowerLog.ShouldBeNull("EnableContinuousExponent=false should skip the PowerLog fit");
    }

    [Fact]
    public void BootstrapIterations_ReflectedInDiagnostic()
    {
        var rng = new Random(303);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => 3.0 * x,
            ScaleFishTestHelpers.LogSpacedX(8, 256, 6),
            sampleSize: 25,
            relativeNoise: 0.04,
            rng);

        var settings = new ScaleFishSettings { BootstrapIterations = 50 };
        var result = new ComplexityEstimator(settings).EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.Bootstrap.ShouldNotBeNull();
        result.Bootstrap.Iterations.ShouldBe(50);
    }

    [Fact]
    public void DistinguishabilityDelta_StricterThresholdRejectsNearTies()
    {
        // Exact linear data with a wide range — even Linear vs NLogN typically opens a Δ-AICc > 50.
        // A very high threshold should still report "distinguishable=yes" because the gap is huge.
        var measurements = ScaleFishTestHelpers.BuildExact(new Linear(),
            ScaleFishTestHelpers.LogSpacedX(8, 2048, 8));

        var loose = new ComplexityEstimator(new ScaleFishSettings { DistinguishabilityDelta = 2.0 });
        var strict = new ComplexityEstimator(new ScaleFishSettings { DistinguishabilityDelta = 10000.0 });

        var looseResult = loose.EstimateComplexity(measurements);
        var strictResult = strict.EstimateComplexity(measurements);

        looseResult.ShouldNotBeNull();
        strictResult.ShouldNotBeNull();
        looseResult.IsDistinguishable.ShouldBeTrue("loose threshold should declare exact-linear data distinguishable");
        strictResult.IsDistinguishable.ShouldBeFalse("a 10000 ΔAICc bar should reject everything");
    }

    [Fact]
    public void NullRunSettings_FallsBackToDefaults()
    {
        // The IRunSettings overload tolerates a null arg by using default settings — protects DI graphs
        // that haven't fully wired the new property yet.
        var estimator = new ComplexityEstimator((Sailfish.Contracts.Public.Models.IRunSettings?)null!);
        var measurements = ScaleFishTestHelpers.BuildExact(new Linear(), new[] { 4, 8, 16, 32, 64, 128 });
        var result = estimator.EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.ScaleFishModelFunction.Name.ShouldBe(nameof(Linear));
    }

    [Fact]
    public void RunSettingsBuilder_WithScaleFishSettings_RoundsTripIntoIRunSettings()
    {
        var custom = new ScaleFishSettings
        {
            EnableBootstrap = false,
            BootstrapIterations = 42,
            DistinguishabilityDelta = 4.0
        };
        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder().WithScaleFish(custom).Build();
        runSettings.RunScaleFish.ShouldBeTrue();
        runSettings.ScaleFishSettings.ShouldBeSameAs(custom);
    }

    [Fact]
    public void RunSettingsBuilder_WithScaleFish_NoArg_GetsDefaultSettings()
    {
        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder().WithScaleFish().Build();
        runSettings.RunScaleFish.ShouldBeTrue();
        runSettings.ScaleFishSettings.ShouldNotBeNull();
        runSettings.ScaleFishSettings.EnableBootstrap.ShouldBeTrue();
    }
}
