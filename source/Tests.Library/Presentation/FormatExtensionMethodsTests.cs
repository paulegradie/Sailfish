using System;
using System.Linq;
using Shouldly;
using Xunit;
using Sailfish.Presentation;
using Sailfish.Execution;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Tests.Common.Builders;

namespace Tests.Library.Presentation;

public class FormatExtensionMethodsTests
{
    [Fact]
    public void ToSummaryFormat_ComputesConfidenceIntervals_AndMapsCoreFields()
    {
        // Arrange: tracking format with clean data (n=4) and stdDev=20 => SE=10
        var testCaseId = new TestCaseId(new TestCaseName(["C", "M"]), new TestCaseVariables([]));
        var tracking = ClassExecutionSummaryTrackingFormatBuilder.Create()
            .WithExecutionSettings(b => b
                .WithAsCsv(true)
                .WithAsConsole(false)
                .WithAsMarkdown(true)
                .WithSampleSize(4)
                .WithNumWarmupIterations(1))
            .WithCompiledTestCaseResult(b => b
                .WithGroupingId("G")
                .WithTestCaseId(testCaseId)
                .WithPerformanceRunResult(
                    PerformanceRunResultTrackingFormatBuilder.Create()
                        .WithDisplayName(testCaseId.DisplayName)
                        .WithMean(100.0)
                        .WithMedian(100.0)
                        .WithStdDev(20.0)
                        .WithVariance(400.0)
                        .WithSampleSize(4)
                        .WithNumWarmupIterations(1)
                        .WithDataWithOutliersRemoved([100.0, 100.0, 100.0, 100.0])
                        .Build())
            )
            .Build();

        // Act
        var summary = tracking.ToSummaryFormat();

        // Assert
        summary.ExecutionSettings.AsCsv.ShouldBeTrue();
        summary.ExecutionSettings.AsMarkdown.ShouldBeTrue();
        summary.ExecutionSettings.AsConsole.ShouldBeFalse();

        var compiled = summary.CompiledTestCaseResults.Single();
        compiled.GroupingId.ShouldBe("G");
        compiled.TestCaseId.ShouldNotBeNull();
        compiled.Exception.ShouldBeNull();
        compiled.PerformanceRunResult.ShouldNotBeNull();

        var perf = compiled.PerformanceRunResult!;
        perf.Mean.ShouldBe(100.0);
        perf.Median.ShouldBe(100.0);
        perf.StdDev.ShouldBe(20.0);
        perf.DataWithOutliersRemoved.Length.ShouldBe(4);
        System.Math.Abs(perf.StandardError - 10.0).ShouldBeLessThan(1e-6);

        // CI list should include 0.95 and 0.99
        perf.ConfidenceIntervals.ShouldNotBeNull();
        perf.ConfidenceIntervals.Count.ShouldBeGreaterThanOrEqualTo(2);
        perf.ConfidenceIntervals.Any(ci => Math.Abs(ci.ConfidenceLevel - 0.95) < 1e-9).ShouldBeTrue();
        perf.ConfidenceIntervals.Any(ci => Math.Abs(ci.ConfidenceLevel - 0.99) < 1e-9).ShouldBeTrue();

        // Convenience properties should be non-zero for n>1
        perf.CI95MarginOfError.ShouldBeGreaterThan(0);
        perf.CI99MarginOfError.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ToTrackingFormat_RoundTripsCoreFields()
    {
        // Arrange: create a simple in-memory summary and round-trip it
        var exec = new ExecutionSettings(asCsv: true, asConsole: true, asMarkdown: false, sampleSize: 11, numWarmupIterations: 7)
        {
            DisableOverheadEstimation = true
        };
        var testCaseId = new TestCaseId(new TestCaseName(["T", "Method"]), new TestCaseVariables([]));
        var perf = new PerformanceRunResult(
            displayName: testCaseId.DisplayName,
            mean: 10,
            stdDev: 2,
            variance: 4,
            median: 10,
            rawExecutionResults: [10.0, 10.0],
            sampleSize: 11,
            numWarmupIterations: 7,
            dataWithOutliersRemoved: [10.0, 10.0],
            upperOutliers: [],
            lowerOutliers: [],
            totalNumOutliers: 0,
            standardError: 0,
            confidenceLevel: 0.95,
            confidenceIntervalLower: 10,
            confidenceIntervalUpper: 10,
            marginOfError: 0,
            confidenceIntervals: []);

        var compiled = new CompiledTestCaseResult(testCaseId, groupingId: "GroupA", performanceRunResult: perf);
        var summary = new ClassExecutionSummary(typeof(FormatExtensionMethodsTests), exec, [compiled]);

        // Act
        var tracking = summary.ToTrackingFormat();

        // Assert: execution settings round trip
        tracking.ExecutionSettings.AsCsv.ShouldBeTrue();
        tracking.ExecutionSettings.AsConsole.ShouldBeTrue();
        tracking.ExecutionSettings.AsMarkdown.ShouldBeFalse();
        tracking.ExecutionSettings.NumWarmupIterations.ShouldBe(7);
        tracking.ExecutionSettings.SampleSize.ShouldBe(11);
        tracking.ExecutionSettings.DisableOverheadEstimation.ShouldBeTrue();

        // And the performance result core fields
        var tr = tracking.CompiledTestCaseResults.Single().PerformanceRunResult!;
        tr.DisplayName.ShouldBe(testCaseId.DisplayName);
        tr.Mean.ShouldBe(10);
        tr.Median.ShouldBe(10);
        tr.StdDev.ShouldBe(2);
        tr.Variance.ShouldBe(4);
        tr.SampleSize.ShouldBe(11);
        tr.NumWarmupIterations.ShouldBe(7);
    }
}

