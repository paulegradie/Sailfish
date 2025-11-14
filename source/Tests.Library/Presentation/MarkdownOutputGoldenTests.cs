using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;
using Sailfish;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.DefaultHandlers.Sailfish;
using Sailfish.Diagnostics.Environment;
using Sailfish.Logging;
using Sailfish.Results;
using Shouldly;
using Tests.Common.Builders;
using Tests.Library.TestUtils;
using Xunit;


namespace Tests.Library.Presentation;

public class MarkdownOutputGoldenTests
{
    private sealed class TestLogger : ILogger
    {
        public void Log(LogLevel level, string template, params object[] values) { }
        public void Log(LogLevel level, Exception ex, string template, params object[] values) { }
    }

    private static (TestRunCompletedNotification Notification, IEnvironmentHealthReportProvider HealthProvider, IRunSettings RunSettings, IReproducibilityManifestProvider ManifestProvider, Sailfish.Execution.ITimerCalibrationResultProvider TimerProvider) CreateDeterministicContext()
    {
        // Fixed timestamp and seed for determinism
        var fixedUtc = new DateTime(2024, 05, 01, 12, 00, 00, DateTimeKind.Utc);
        var tempDir = Path.Combine(Path.GetTempPath(), "Sailfish_GoldenMd_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var runSettings = RunSettingsBuilder.CreateBuilder()
            .WithTimeStamp(fixedUtc)
            .WithLocalOutputDirectory(tempDir)
            .WithArg("seed", "12345")
            .WithTag("env", "local")
            .Build();

        var healthProvider = new EnvironmentHealthReportProvider
        {
            Current = new EnvironmentHealthReport(new List<HealthCheckEntry>
            {
                new("Build Mode", HealthStatus.Pass, "Release"),
                new("JIT (Tiered/OSR)", HealthStatus.Pass, "Tiered JIT: enabled; OSR: enabled"),
                new("Background CPU Load", HealthStatus.Warn, "~8% idle")
            })
        };

        var timerProvider = new TestTimerProvider
        {
            Current = new Sailfish.Execution.TimerCalibrationResult
            {
                StopwatchFrequency = 3_000_000,
                ResolutionNs = 333.3,
                MedianTicks = 4,
                RsdPercent = 2.2,
                JitterScore = 91,
                Samples = 64,
                Warmups = 16
            }
        };

        var manifestProvider = new ReproducibilityManifestProvider
        {
            Current = ReproducibilityManifest.CreateBase(runSettings, healthProvider.Current)
        };
        // Attach calibration to manifest for markdown summary section
        manifestProvider.Current!.TimerCalibration = ReproducibilityManifest.TimerCalibrationSnapshot.From(timerProvider.Current!);

        // Build deterministic execution summaries: one class with WriteToMarkdown attribute
        var dataLen = 50;
        var zeros = Enumerable.Repeat(0.0, dataLen).ToArray();

        var classSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestTypes.MarkdownGoldenClass))
            // Comparison group G1: A (100), B (110), C (101)
            .WithCompiledTestCaseResult(b => b
                .WithGroupingId("G1")
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("A").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(100.0).WithStdDev(4.0).WithSampleSize(dataLen)
                    .WithDataWithOutliersRemoved(zeros).WithNumWarmupIterations(5)
                    .Build()))
            .WithCompiledTestCaseResult(b => b
                .WithGroupingId("G1")
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("B").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(110.0).WithStdDev(4.5).WithSampleSize(dataLen)
                    .WithDataWithOutliersRemoved(zeros).WithNumWarmupIterations(5)
                    .Build()))
            .WithCompiledTestCaseResult(b => b
                .WithGroupingId("G1")
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("C").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(101.0).WithStdDev(4.1).WithSampleSize(dataLen)
                    .WithDataWithOutliersRemoved(zeros).WithNumWarmupIterations(5)
                    .Build()))
            // A standalone method (not in comparison group) to populate Individual Results
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Standalone").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(50.0).WithStdDev(2.5).WithSampleSize(dataLen)
                    .WithDataWithOutliersRemoved(zeros).WithNumWarmupIterations(3)
                    .Build()))
            .Build();

        var notification = new TestRunCompletedNotification(new List<ClassExecutionSummaryTrackingFormat> { classSummary });

        return (notification, healthProvider, runSettings, manifestProvider, timerProvider);
    }

    [Fact]
    public async Task Consolidated_Session_Markdown_Matches_Golden()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var (notification, healthProvider, runSettings, manifestProvider, timerProvider) = CreateDeterministicContext();

            var mediator = Substitute.For<IMediator>();
            string? actualMarkdown = null;
            mediator
                .When(m => m.Publish(Arg.Any<WriteMethodComparisonMarkdownNotification>(), Arg.Any<CancellationToken>()))
                .Do(ci => actualMarkdown = ci.ArgAt<WriteMethodComparisonMarkdownNotification>(0).MarkdownContent);

            var logger = new TestLogger();
            var handler = new MethodComparisonTestRunCompletedHandler(
                logger,
                mediator,
                healthProvider,
                runSettings,
                manifestProvider,
                timerProvider);

            await handler.Handle(notification, CancellationToken.None);

            await mediator.Received(1).Publish(Arg.Any<WriteMethodComparisonMarkdownNotification>(), Arg.Any<CancellationToken>());
            actualMarkdown.ShouldNotBeNullOrWhiteSpace();

            // Debug: write actual markdown to temp file
            var debugPath = Path.Combine(Path.GetTempPath(), "actual_markdown_debug.txt");
            File.WriteAllText(debugPath, actualMarkdown!);

            var actualNormalized = GoldenNormalization.NormalizeMarkdown(actualMarkdown!);

            var projectDir = GetProjectDirectory();
            var goldenDir = Path.Combine(projectDir, "TestResources", "Golden");
            Directory.CreateDirectory(goldenDir);
            var goldenPath = Path.Combine(goldenDir, "ConsolidatedSession.md");

            if (!File.Exists(goldenPath))
            {
                File.WriteAllText(goldenPath, actualNormalized);
                throw new Xunit.Sdk.XunitException($"Golden file was missing for markdown. Created at {goldenPath}. Review and re-run tests.");
            }

            var expected = File.ReadAllText(goldenPath);
            var expectedNormalized = GoldenNormalization.NormalizeMarkdown(expected);

            // Use similarity-based comparison (95% threshold) to be resilient to minor platform-specific differences
            var similarity = GoldenNormalization.CalculateSimilarityPercentage(actualNormalized, expectedNormalized);
            const double similarityThreshold = 95.0;

            if (similarity < similarityThreshold)
            {
                var diffReport = GoldenNormalization.GenerateDiffReport(actualNormalized, expectedNormalized);
                throw new Xunit.Sdk.XunitException(
                    $"Markdown output similarity is {similarity:F2}% (threshold: {similarityThreshold}%)\n\n{diffReport}\n\n" +
                    $"Expected:\n{expectedNormalized}\n\n" +
                    $"Actual:\n{actualNormalized}");
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static string GetProjectDirectory()
    {
        // Resolve to source/Tests.Library
        var baseDir = AppContext.BaseDirectory; // .../bin/<cfg>/<tfm>/
        var dir = Directory.GetParent(baseDir)!; // tfm
        dir = dir.Parent!; // cfg
        dir = dir.Parent!; // bin
        dir = dir.Parent!; // project
        return dir.FullName;
    }

    // Minimal test types decorated for WriteToMarkdown discovery
    private static class TestTypes
    {
        [Sailfish.Attributes.Sailfish]
        [Sailfish.Attributes.WriteToMarkdown]
        public class MarkdownGoldenClass { }
    }

    private sealed class TestTimerProvider : Sailfish.Execution.ITimerCalibrationResultProvider
    {
        public Sailfish.Execution.TimerCalibrationResult? Current { get; set; }
    }
}

