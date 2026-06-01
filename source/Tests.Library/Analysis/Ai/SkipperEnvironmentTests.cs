using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish.Analysis.Ai;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Diagnostics.Environment;
using Sailfish.Results;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.Ai;

public class SkipperEnvironmentTests
{
    [Fact]
    public void Environment_IsNull_WhenNeitherManifestNorHealthCaptured()
    {
        var context = MakeBuilder(manifest: null, health: null).Build(Notification(), 0.05);

        context.Environment.ShouldBeNull();
    }

    [Fact]
    public void Environment_ProjectsManifestFields_AndFiltersConcernsToWarnAndFail()
    {
        var manifest = new ReproducibilityManifest
        {
            DotNetRuntime = ".NET 10.0",
            Os = "macOS 15",
            CpuModel = "Apple M3",
            GcMode = "Workstation",
            CiSystem = "GitHub Actions",
            CommitSha = "abc123",
            EnvironmentHealthScore = 72,
            EnvironmentHealthLabel = "Good"
        };
        var health = new EnvironmentHealthReport(new[]
        {
            new HealthCheckEntry("Power Plan", HealthStatus.Warn, "Balanced", "Switch to High Performance"),
            new HealthCheckEntry("Tiered PGO", HealthStatus.Pass, "disabled"),
            new HealthCheckEntry("Debugger", HealthStatus.Fail, "attached", "Detach the debugger")
        });

        var context = MakeBuilder(manifest, health).Build(Notification(), 0.05);

        context.Environment.ShouldNotBeNull();
        var env = context.Environment!;
        env.DotNetRuntime.ShouldBe(".NET 10.0");
        env.CpuModel.ShouldBe("Apple M3");
        env.CiSystem.ShouldBe("GitHub Actions");
        env.CommitSha.ShouldBe("abc123");
        env.HealthScore.ShouldBe(72);
        env.HealthLabel.ShouldBe("Good");

        // Only Warn/Fail entries are surfaced as concerns — Pass is noise for reliability judgement.
        env.Concerns.Select(c => c.Name).ShouldBe(new[] { "Power Plan", "Debugger" }, ignoreOrder: true);
        env.Concerns.Single(c => c.Name == "Power Plan").Recommendation.ShouldBe("Switch to High Performance");
    }

    [Fact]
    public void Environment_UsesHealthReportScore_WhenManifestAbsent()
    {
        var health = new EnvironmentHealthReport(new[]
        {
            new HealthCheckEntry("Timer", HealthStatus.Pass, "high-resolution")
        });

        var context = MakeBuilder(manifest: null, health: health).Build(Notification(), 0.05);

        context.Environment.ShouldNotBeNull();
        context.Environment!.HealthScore.ShouldBe(health.Score);
        context.Environment.Concerns.ShouldBeEmpty();
    }

    private static PerformanceNarrativeContextBuilder MakeBuilder(ReproducibilityManifest? manifest, EnvironmentHealthReport? health)
    {
        var manifestProvider = Substitute.For<IReproducibilityManifestProvider>();
        manifestProvider.Current.Returns(manifest);
        var healthProvider = Substitute.For<IEnvironmentHealthReportProvider>();
        healthProvider.Current.Returns(health);
        return new PerformanceNarrativeContextBuilder(manifestProvider, healthProvider);
    }

    private static SailDiffAnalysisCompleteNotification Notification()
    {
        var stats = new StatisticalTestResult(
            10, 11, 10, 11, 0, 0.01, "desc", 5, 5,
            Array.Empty<double>(), Array.Empty<double>(), new Dictionary<string, object>());
        var result = new SailDiffResult(new TestCaseId("X"), new TestResultWithOutlierAnalysis(stats, null, null));
        return new SailDiffAnalysisCompleteNotification(new[] { result }, "## md");
    }
}
