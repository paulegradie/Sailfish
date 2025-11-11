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
using Xunit.Sdk;


namespace Tests.Library.Presentation;

public class CsvOutputGoldenTests
{
    private sealed class TestLogger : ILogger
    {
        public void Log(LogLevel level, string template, params object[] values) { }
        public void Log(LogLevel level, Exception ex, string template, params object[] values) { }
    }

    private static TestRunCompletedNotification CreateNotification()
    {
        var dataLen = 40;
        double[] zeros = Enumerable.Repeat(0.0, dataLen).ToArray();

        var classSummary = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithTestClass(typeof(TestTypes.CsvGoldenClass))
            // Comparison group G1: A (80), B (100), C (82)
            .WithCompiledTestCaseResult(b => b
                .WithGroupingId("G1")
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("A").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(80.0).WithStdDev(3.5).WithSampleSize(dataLen)
                    .WithDataWithOutliersRemoved(zeros).WithNumWarmupIterations(4)
                    .Build()))
            .WithCompiledTestCaseResult(b => b
                .WithGroupingId("G1")
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("B").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(100.0).WithStdDev(4.0).WithSampleSize(dataLen)
                    .WithDataWithOutliersRemoved(zeros).WithNumWarmupIterations(4)
                    .Build()))
            .WithCompiledTestCaseResult(b => b
                .WithGroupingId("G1")
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("C").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(82.0).WithStdDev(3.6).WithSampleSize(dataLen)
                    .WithDataWithOutliersRemoved(zeros).WithNumWarmupIterations(4)
                    .Build()))
            // Standalone method
            .WithCompiledTestCaseResult(b => b
                .WithTestCaseId(TestCaseIdBuilder.Create().WithTestCaseName("Solo").Build())
                .WithPerformanceRunResult(PerformanceRunResultTrackingFormatBuilder.Create()
                    .WithMean(33.0).WithStdDev(1.2).WithSampleSize(dataLen)
                    .WithDataWithOutliersRemoved(zeros).WithNumWarmupIterations(2)
                    .Build()))
            .Build();

        return new TestRunCompletedNotification(new List<ClassExecutionSummaryTrackingFormat> { classSummary });
    }

    [Fact]
    public async Task Consolidated_Session_Csv_Matches_Golden()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            var mediator = Substitute.For<IMediator>();
            string? actualCsv = null;
            mediator
                .When(m => m.Publish(Arg.Any<WriteMethodComparisonCsvNotification>(), Arg.Any<CancellationToken>()))
                .Do(ci => actualCsv = ci.ArgAt<WriteMethodComparisonCsvNotification>(0).CsvContent);

            var logger = new TestLogger();
            var handler = new CsvTestRunCompletedHandler(logger, mediator);

            var notification = CreateNotification();
            await handler.Handle(notification, CancellationToken.None);

            await mediator.Received(1).Publish(Arg.Any<WriteMethodComparisonCsvNotification>(), Arg.Any<CancellationToken>());
            actualCsv.ShouldNotBeNullOrWhiteSpace();

            var actualNormalized = GoldenNormalization.NormalizeCsv(actualCsv!);

            var projectDir = GetProjectDirectory();
            var goldenDir = Path.Combine(projectDir, "TestResources", "Golden");
            Directory.CreateDirectory(goldenDir);
            var goldenPath = Path.Combine(goldenDir, "ConsolidatedSession.csv");

            if (!File.Exists(goldenPath))
            {
                File.WriteAllText(goldenPath, actualNormalized);
                throw new Xunit.Sdk.XunitException($"Golden file was missing for csv. Created at {goldenPath}. Review and re-run tests.");
            }

            var expected = File.ReadAllText(goldenPath);
            var expectedNormalized = GoldenNormalization.NormalizeCsv(expected);

            actualNormalized.ShouldBe(expectedNormalized);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    private static string GetProjectDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        var dir = Directory.GetParent(baseDir)!; // tfm
        dir = dir.Parent!; // cfg
        dir = dir.Parent!; // bin
        dir = dir.Parent!; // project
        return dir.FullName;
    }

    private static class TestTypes
    {
        [Sailfish.Attributes.Sailfish]
        [Sailfish.Attributes.WriteToCsv]
        public class CsvGoldenClass { }
    }
}

