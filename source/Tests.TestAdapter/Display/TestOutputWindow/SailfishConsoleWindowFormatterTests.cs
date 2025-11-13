using System;
using System.Collections.Generic;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Logging;
using Sailfish.TestAdapter.Display.TestOutputWindow;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter.Display.TestOutputWindow;

public class SailfishConsoleWindowFormatterTests
{
    [Fact]
    public void FormConsoleWindowMessage_IncludesValidationWarnings()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var formatter = new SailfishConsoleWindowFormatter(logger);

        var warnings = new ValidationResult(new[]
        {
            new ValidationWarning("LOW_SAMPLE_SIZE", "Only 3 effective samples after outlier removal; estimates may be unstable.", ValidationSeverity.Warning),
            new ValidationWarning("ELEVATED_OUTLIERS", "Outliers comprise 20% of samples.", ValidationSeverity.Warning)
        });

        var pr = new PerformanceRunResult(
            displayName: "MyTest",
            mean: 10,
            stdDev: 1,
            variance: 1,
            median: 10,
            rawExecutionResults: new[] { 9.0, 10.0, 11.0, 100.0 },
            sampleSize: 4,
            numWarmupIterations: 0,
            dataWithOutliersRemoved: new[] { 9.0, 10.0, 11.0 },
            upperOutliers: new[] { 100.0 },
            lowerOutliers: Array.Empty<double>(),
            totalNumOutliers: 1,
            standardError: 0.5,
            confidenceLevel: 0.95,
            confidenceIntervalLower: 9,
            confidenceIntervalUpper: 11,
            marginOfError: 1.0
        )
        {
            Validation = warnings
        };

        var compiled = new StubCompiledResult(new TestCaseId("MyTest"), string.Empty, pr);
        var summary = new StubClassExecutionSummary(typeof(object), new ExecutionSettings(), new[] { compiled });

        // Act
        var output = formatter.FormConsoleWindowMessageForSailfish(new[] { summary });

        // Assert
        output.ShouldContain("Warnings");
        output.ShouldContain("Only 3 effective samples");
        output.ShouldContain("Outliers comprise 20% of samples.");
    }

    private sealed class StubCompiledResult : ICompiledTestCaseResult
    {
        public StubCompiledResult(TestCaseId id, string grouping, PerformanceRunResult pr)
        {
            TestCaseId = id;
            GroupingId = grouping;
            PerformanceRunResult = pr;
        }
        public string? GroupingId { get; }
        public PerformanceRunResult? PerformanceRunResult { get; }
        public Exception? Exception { get; } = null;
        public TestCaseId? TestCaseId { get; }
    }

    private sealed class StubClassExecutionSummary : IClassExecutionSummary
    {
        public StubClassExecutionSummary(Type testClass, IExecutionSettings settings, IEnumerable<ICompiledTestCaseResult> results)
        {
            TestClass = testClass;
            ExecutionSettings = settings;
            CompiledTestCaseResults = results;
        }
        public Type TestClass { get; }
        public IExecutionSettings ExecutionSettings { get; }
        public IEnumerable<ICompiledTestCaseResult> CompiledTestCaseResults { get; }
        public IEnumerable<ICompiledTestCaseResult> GetSuccessfulTestCases() => CompiledTestCaseResults;
        public IEnumerable<ICompiledTestCaseResult> GetFailedTestCases() => Array.Empty<ICompiledTestCaseResult>();
        public IClassExecutionSummary FilterForSuccessfulTestCases() => this;
        public IClassExecutionSummary FilterForFailureTestCases() => this;
    }
}

