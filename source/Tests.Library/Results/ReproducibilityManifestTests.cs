using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sailfish.Diagnostics.Environment;
using Sailfish.Results;
using Shouldly;
using Tests.Common.Builders;
using Xunit;

namespace Tests.Library.Results
{
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
    }
}

