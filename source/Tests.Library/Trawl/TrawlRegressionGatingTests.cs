using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Logging;
using Shouldly;
using Xunit;

namespace Tests.Library.Trawl;

/// <summary>
/// Drives a [Trawl] scenario through the iterator with a seeded baseline and a stubbed statistical
/// executor, proving the regression gate fails (or not) the case based on TrawlSettings.FailOnRegression.
/// </summary>
public class TrawlRegressionGatingTests
{
    [Fact]
    public async Task FailOnRegression_FailsTheCase_WhenRegressed()
    {
        var result = await RunWithSeededBaseline(failOnRegression: true);

        result.IsSuccess.ShouldBeFalse();
        result.Exception.ShouldNotBeNull();
        result.Exception!.Message.ShouldContain("regression");
    }

    [Fact]
    public async Task WithoutGate_RegressionIsReportedButCasePasses()
    {
        var result = await RunWithSeededBaseline(failOnRegression: false);

        result.IsSuccess.ShouldBeTrue();
    }

    private static async Task<TestCaseExecutionResult> RunWithSeededBaseline(bool failOnRegression)
    {
        var dir = Path.Combine(Path.GetTempPath(), "trawl_gate_" + Guid.NewGuid().ToString("N"));
        var logger = Substitute.For<ILogger>();

        var instance = new GateWork();
        var method = typeof(GateWork).GetMethod(nameof(GateWork.Scenario))!;
        var execSettings = new ExecutionSettings { NumWarmupIterations = 0, SampleSize = 1, UseAdaptiveSampling = false };
        var container = TestInstanceContainer.CreateTestInstance(instance, method, Array.Empty<string>(), Array.Empty<object>(), false, execSettings);

        // Seed a baseline for this scenario so the engine has something to compare against.
        new TrawlResultWriter().PersistRecord(
            new TrawlResult { DisplayName = container.TestCaseId.DisplayName, LatencySamplesMs = new[] { 1.0, 2, 3, 4, 5 } },
            DateTime.UtcNow.AddMinutes(-5), dir);

        // Stub the statistical executor to report a significant slowdown (10ms -> 20ms).
        var stat = new StatisticalTestResult(10, 20, 10, 20, 0, 0.001, "change", 5, 5,
            new double[0], new double[0], new Dictionary<string, object>());
        var exec = Substitute.For<IStatisticalTestExecutor>();
        exec.ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(new TestResultWithOutlierAnalysis(stat, null, null));

        var runSettings = Sailfish.RunSettingsBuilder.CreateBuilder()
            .WithLocalOutputDirectory(dir)
            .WithTrawl(new Sailfish.Trawl.TrawlSettings { FailOnRegression = failOnRegression })
            .Build();

        var iterator = new TestCaseIterator(runSettings, logger,
            new FixedIterationStrategy(logger),
            new AdaptiveIterationStrategy(logger, Substitute.For<IStatisticalConvergenceDetector>()),
            exec);

        try
        {
            return await iterator.Iterate(container, disableOverheadEstimation: true, CancellationToken.None);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { /* ignore */ }
        }
    }

    [Sailfish]
    private sealed class GateWork
    {
        [Trawl(VirtualUsers = 2, DurationSeconds = 0.2, WarmupSeconds = 0)]
        public async Task Scenario(CancellationToken ct) => await Task.Delay(2, ct);
    }
}
