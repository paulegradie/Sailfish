using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sailfish.Diagnostics.Environment;
using Sailfish.Results;
using Shouldly;
using Tests.Common.Builders;
using Xunit;

namespace Tests.Library.Results;

public class ReproducibilityManifestTests
{
    [Fact]
    public void CreateBase_MapsCoreFields_FromRunSettings_AndHealth()
    {
        // Arrange
        var ts = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder()
            .WithTimeStamp(ts)
            .WithLocalOutputDirectory("out")
            .WithTag("env", "test")
            .Build();

        var health = new EnvironmentHealthReport(new List<HealthCheckEntry>
        {
            new("Build Mode", HealthStatus.Pass, "Release mode")
        });

        // Act
        var manifest = ReproducibilityManifest.CreateBase(runSettings, health);

        // Assert
        manifest.ShouldNotBeNull();
        manifest.DotNetRuntime.ShouldNotBeNullOrWhiteSpace();
        manifest.OS.ShouldNotBeNullOrWhiteSpace();
        manifest.GCMode.ShouldNotBeNullOrWhiteSpace();
        manifest.Jit.ShouldNotBeNullOrWhiteSpace();
        manifest.ProcessPriority.ShouldNotBeNullOrWhiteSpace();
        manifest.Timer.ShouldNotBeNullOrWhiteSpace();
        manifest.SessionId.ShouldNotBeNullOrWhiteSpace();
        manifest.TimestampUtc.ShouldBeGreaterThan(DateTime.MinValue);
        manifest.EnvironmentHealthScore.ShouldBe(health.Score);
        manifest.EnvironmentHealthLabel.ShouldBe(health.SummaryLabel);
        manifest.Tags.ShouldContainKey("env");
        manifest.Tags["env"].ShouldBe("test");
    }

    [Fact]
    public void CreateBase_SetsRandomizationSeed_WhenSeedProvided()
    {
        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder()
            .WithSeed(42)
            .Build();

        var manifest = ReproducibilityManifest.CreateBase(runSettings, null);

        manifest.Randomization.ShouldNotBeNull();
        manifest.Randomization.Seed.ShouldBe(42);
        manifest.Randomization.Types.ShouldBeTrue();
        manifest.Randomization.Methods.ShouldBeTrue();
        manifest.Randomization.PropertySets.ShouldBeTrue();
    }

    [Fact]
    public void CreateBase_LeavesRandomizationUnset_WhenNoSeed()
    {
        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder().Build();

        var manifest = ReproducibilityManifest.CreateBase(runSettings, null);

        manifest.Randomization.ShouldNotBeNull();
        manifest.Randomization.Seed.ShouldBeNull();
        manifest.Randomization.Types.ShouldBeFalse();
        manifest.Randomization.Methods.ShouldBeFalse();
        manifest.Randomization.PropertySets.ShouldBeFalse();
    }


    [Fact]
    public void AddMethodSnapshots_MapsFields_AndComputesApproximateCIMargins()
    {
        // Arrange: build tracking formats with n=4, mean=100, stddev=20
        var testCaseId = TestCaseIdBuilder.Create().WithTestCaseName("MyTest").Build();
        var perf = PerformanceRunResultTrackingFormatBuilder.Create()
            .WithDisplayName(testCaseId.DisplayName)
            .WithMean(100.0)
            .WithMedian(100.0)
            .WithStdDev(20.0)
            .WithNumWarmupIterations(2)
            .WithDataWithOutliersRemoved(new double[] { 1, 2, 3, 4 })
            .Build();

        var classSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(testCaseId)
                .WithPerformanceRunResult(perf))
            .Build();

        var manifest = new ReproducibilityManifest();

        // Act
        manifest.AddMethodSnapshots(new[] { classSummary });

        // Assert
        manifest.Methods.Count.ShouldBe(1);
        var m = manifest.Methods.Single();
        m.TestCaseDisplayName.ShouldBe(testCaseId.DisplayName);
        m.SampleSize.ShouldBe(4);
        m.NumWarmupIterations.ShouldBe(2);
        m.Mean.ShouldBe(100.0);
        m.StdDev.ShouldBe(20.0);
        m.CI95_MarginOfError.ShouldNotBeNull();
        m.CI99_MarginOfError.ShouldNotBeNull();
        // Rough sanity checks: with n=4, se=10, t95~3.18 => ~31.8; t99~5.84 => ~58.4
        m.CI95_MarginOfError!.Value.ShouldBeInRange(28.0, 35.0);
        m.CI99_MarginOfError!.Value.ShouldBeInRange(50.0, 65.0);
    }

    [Fact]
    public void AddMethodSnapshots_WithNullDataWithOutliersRemoved_FallsBackToSampleSize()
    {
        // Arrange: build tracking format with null DataWithOutliersRemoved but valid SampleSize
        var testCaseId = TestCaseIdBuilder.Create().WithTestCaseName("MyTest").Build();
        var perf = PerformanceRunResultTrackingFormatBuilder.Create()
            .WithDisplayName(testCaseId.DisplayName)
            .WithMean(100.0)
            .WithMedian(100.0)
            .WithStdDev(20.0)
            .WithNumWarmupIterations(2)
            .WithSampleSize(5)
            .WithDataWithOutliersRemoved(null!)
            .Build();

        var classSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(testCaseId)
                .WithPerformanceRunResult(perf))
            .Build();

        var manifest = new ReproducibilityManifest();

        // Act
        manifest.AddMethodSnapshots(new[] { classSummary });

        // Assert
        manifest.Methods.Count.ShouldBe(1);
        var m = manifest.Methods.Single();
        m.TestCaseDisplayName.ShouldBe(testCaseId.DisplayName);
        m.SampleSize.ShouldBe(5); // Should fall back to SampleSize when DataWithOutliersRemoved is null
        m.NumWarmupIterations.ShouldBe(2);
        m.Mean.ShouldBe(100.0);
        m.StdDev.ShouldBe(20.0);
        m.CI95_MarginOfError.ShouldNotBeNull();
        m.CI99_MarginOfError.ShouldNotBeNull();
        // Rough sanity checks: with n=5, se=8.94, t95~2.78 => ~24.9; t99~4.60 => ~41.1
        m.CI95_MarginOfError!.Value.ShouldBeInRange(20.0, 30.0);
        m.CI99_MarginOfError!.Value.ShouldBeInRange(35.0, 50.0);
    }

    [Fact]
    public void WriteJson_WritesIndentedJson_ToSpecifiedDirectory()
    {
        // Arrange
        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder().Build();
        var manifest = ReproducibilityManifest.CreateBase(runSettings, null);
        var tempDir = Path.Combine(Path.GetTempPath(), "sailfish_manifest_tests_" + Guid.NewGuid().ToString("N"));
        var fileName = "test-manifest.json";

        try
        {
            // Act
            ReproducibilityManifest.WriteJson(manifest, tempDir, fileName);

            // Assert
            var path = Path.Combine(tempDir, fileName);
            File.Exists(path).ShouldBeTrue();
            new FileInfo(path).Length.ShouldBeGreaterThan(10);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
            }
        }
    }


    [Fact]
    public void TimerCalibrationSnapshot_From_MapsAllFields()
    {
        var r = new Sailfish.Execution.TimerCalibrationResult
        {
            StopwatchFrequency = 1_000_000,
            ResolutionNs = 1000.0,
            BaselineOverheadTicks = 5,
            Warmups = 16,
            Samples = 64,
            StdDevTicks = 0.5,
            MedianTicks = 4,
            RsdPercent = 2.5,
            JitterScore = 90
        };

        var snap = ReproducibilityManifest.TimerCalibrationSnapshot.From(r);
        snap.StopwatchFrequency.ShouldBe(r.StopwatchFrequency);
        snap.ResolutionNs.ShouldBe(r.ResolutionNs);
        snap.BaselineOverheadTicks.ShouldBe(r.BaselineOverheadTicks);
        snap.Warmups.ShouldBe(r.Warmups);
        snap.Samples.ShouldBe(r.Samples);
        snap.StdDevTicks.ShouldBe(r.StdDevTicks);
        snap.MedianTicks.ShouldBe(r.MedianTicks);
        snap.RsdPercent.ShouldBe(r.RsdPercent);
        snap.JitterScore.ShouldBe(r.JitterScore);
    }

    [Fact]
    public void TimerCalibrationSnapshot_Serializes_And_Deserializes()
    {
        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder().Build();
        var manifest = ReproducibilityManifest.CreateBase(runSettings, null);
        manifest.TimerCalibration = new ReproducibilityManifest.TimerCalibrationSnapshot
        {
            StopwatchFrequency = 2_000_000,
            ResolutionNs = 500.0,
            BaselineOverheadTicks = 3,
            Warmups = 8,
            Samples = 32,
            StdDevTicks = 0.2,
            MedianTicks = 3,
            RsdPercent = 1.2,
            JitterScore = 98
        };

        var json = System.Text.Json.JsonSerializer.Serialize(manifest);
        var roundTrip = System.Text.Json.JsonSerializer.Deserialize<ReproducibilityManifest>(json);

        roundTrip.ShouldNotBeNull();
        roundTrip!.TimerCalibration.ShouldNotBeNull();
        roundTrip.TimerCalibration!.StopwatchFrequency.ShouldBe(2_000_000);
        roundTrip.TimerCalibration!.ResolutionNs.ShouldBe(500.0);
        roundTrip.TimerCalibration!.BaselineOverheadTicks.ShouldBe(3);
        roundTrip.TimerCalibration!.Warmups.ShouldBe(8);
        roundTrip.TimerCalibration!.Samples.ShouldBe(32);
        roundTrip.TimerCalibration!.StdDevTicks.ShouldBe(0.2);
        roundTrip.TimerCalibration!.MedianTicks.ShouldBe(3);
        roundTrip.TimerCalibration!.RsdPercent.ShouldBe(1.2);
        roundTrip.TimerCalibration!.JitterScore.ShouldBe(98);
    }

}