using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.TestAdapter.Display.TestOutputWindow;
using Shouldly;
using Xunit;

namespace Tests.TestAdapter;

/// <summary>
/// Comprehensive unit tests for SailfishConsoleWindowFormatter.
/// Tests console output formatting, exception handling, and edge cases.
/// </summary>
public class SailfishConsoleWindowFormatterTests
{
    private readonly ILogger _logger;
    private readonly SailfishConsoleWindowFormatter _formatter;

    public SailfishConsoleWindowFormatterTests()
    {
        _logger = Substitute.For<ILogger>();
        _formatter = new SailfishConsoleWindowFormatter(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ShouldCreateInstance()
    {
        // Act & Assert - Constructor doesn't validate null logger
        var formatter = new SailfishConsoleWindowFormatter(null!);
        formatter.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldCreateInstance()
    {
        // Act
        var formatter = new SailfishConsoleWindowFormatter(_logger);

        // Assert
        formatter.ShouldNotBeNull();
    }

    #endregion

    #region FormConsoleWindowMessageForSailfish Tests

    [Fact]
    public void FormConsoleWindowMessageForSailfish_WithEmptyResults_ShouldReturnNoResultsMessage()
    {
        // Arrange
        var results = new List<IClassExecutionSummary>();

        // Act
        var output = _formatter.FormConsoleWindowMessageForSailfish(results);

        // Assert
        output.ShouldBe("No results to report");
    }

    [Fact]
    public void FormConsoleWindowMessageForSailfish_WithNullCompiledResults_ShouldReturnNoResultsMessage()
    {
        // Arrange
        var executionSummary = Substitute.For<IClassExecutionSummary>();
        executionSummary.CompiledTestCaseResults.Returns(new List<ICompiledTestCaseResult>());
        var results = new List<IClassExecutionSummary> { executionSummary };

        // Act
        var output = _formatter.FormConsoleWindowMessageForSailfish(results);

        // Assert
        output.ShouldBe("No results to report");
    }

    [Fact]
    public void FormConsoleWindowMessageForSailfish_WithException_ShouldReturnExceptionDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var compiledResult = CreateCompiledResultWithException(exception);
        var executionSummary = CreateExecutionSummaryWithCompiledResult(compiledResult);
        var results = new List<IClassExecutionSummary> { executionSummary };

        // Act
        var output = _formatter.FormConsoleWindowMessageForSailfish(results);

        // Assert
        output.ShouldContain("Exception Encountered");
        output.ShouldContain("Test exception");
        output.ShouldContain("StackTrace:");
        _logger.Received().Log(LogLevel.Error, Arg.Any<string>());
    }

    [Fact]
    public void FormConsoleWindowMessageForSailfish_WithInnerException_ShouldIncludeInnerExceptionDetails()
    {
        // Arrange
        var innerException = new ArgumentException("Inner exception");
        var exception = new InvalidOperationException("Outer exception", innerException);
        var compiledResult = CreateCompiledResultWithException(exception);
        var executionSummary = CreateExecutionSummaryWithCompiledResult(compiledResult);
        var results = new List<IClassExecutionSummary> { executionSummary };

        // Act
        var output = _formatter.FormConsoleWindowMessageForSailfish(results);

        // Assert
        output.ShouldContain("Inner Stack Trace");
        output.ShouldContain("Inner exception");
        output.ShouldContain("Outer exception");
    }

    [Fact]
    public void FormConsoleWindowMessageForSailfish_WithValidResults_ShouldReturnFormattedTable()
    {
        // Arrange
        var performanceResult = CreatePerformanceRunResult();
        var compiledResult = CreateCompiledResultWithPerformance(performanceResult);
        var executionSummary = CreateExecutionSummaryWithCompiledResult(compiledResult);
        var results = new List<IClassExecutionSummary> { executionSummary };

        // Act
        var output = _formatter.FormConsoleWindowMessageForSailfish(results);

        // Assert
        output.ShouldNotBeNullOrEmpty();
        output.ShouldContain("Descriptive Statistics");
        output.ShouldContain("Mean");
        output.ShouldContain("Median");
        output.ShouldContain("StdDev");
        output.ShouldContain("Min");
        output.ShouldContain("Max");
        _logger.Received().Log(LogLevel.Information, Arg.Any<string>(), Arg.Any<object[]>());
    }

    [Fact]
    public void FormConsoleWindowMessageForSailfish_WithUpperOutliers_ShouldIncludeOutlierInformation()
    {
        // Arrange
        var performanceResult = CreatePerformanceRunResultWithUpperOutliers();
        var compiledResult = CreateCompiledResultWithPerformance(performanceResult);
        var executionSummary = CreateExecutionSummaryWithCompiledResult(compiledResult);
        var results = new List<IClassExecutionSummary> { executionSummary };

        // Act
        var output = _formatter.FormConsoleWindowMessageForSailfish(results);

        // Assert
        output.ShouldContain("Upper Outliers:");
    }

    [Fact]
    public void FormConsoleWindowMessageForSailfish_WithLowerOutliers_ShouldIncludeOutlierInformation()
    {
        // Arrange
        var performanceResult = CreatePerformanceRunResultWithLowerOutliers();
        var compiledResult = CreateCompiledResultWithPerformance(performanceResult);
        var executionSummary = CreateExecutionSummaryWithCompiledResult(compiledResult);
        var results = new List<IClassExecutionSummary> { executionSummary };

        // Act
        var output = _formatter.FormConsoleWindowMessageForSailfish(results);

        // Assert
        output.ShouldContain("Lower Outliers:");
    }

    [Fact]
    public void FormConsoleWindowMessageForSailfish_WithEmptyPerformanceResult_ShouldReturnEmptyString()
    {
        // Arrange
        var performanceResult = CreateEmptyPerformanceRunResult();
        var compiledResult = CreateCompiledResultWithPerformance(performanceResult);
        var executionSummary = CreateExecutionSummaryWithCompiledResult(compiledResult);
        var results = new List<IClassExecutionSummary> { executionSummary };

        // Act
        var output = _formatter.FormConsoleWindowMessageForSailfish(results);

        // Assert
        output.ShouldBeEmpty();
    }

    [Fact]
    public void FormConsoleWindowMessageForSailfish_WithTags_ShouldProcessSuccessfully()
    {
        // Arrange
        var performanceResult = CreatePerformanceRunResult();
        var compiledResult = CreateCompiledResultWithPerformance(performanceResult);
        var executionSummary = CreateExecutionSummaryWithCompiledResult(compiledResult);
        var results = new List<IClassExecutionSummary> { executionSummary };
        var tags = new OrderedDictionary { ["Environment"] = "Test", ["Version"] = "1.0" };

        // Act
        var output = _formatter.FormConsoleWindowMessageForSailfish(results, tags);

        // Assert
        output.ShouldNotBeNullOrEmpty();
        output.ShouldContain("Descriptive Statistics");
    }

    #endregion

    #region Helper Methods

    private ICompiledTestCaseResult CreateCompiledResultWithException(Exception exception)
    {
        var result = Substitute.For<ICompiledTestCaseResult>();
        result.Exception.Returns(exception);
        result.TestCaseId.Returns(new TestCaseId("TestClass.TestMethod()"));
        return result;
    }

    private ICompiledTestCaseResult CreateCompiledResultWithPerformance(PerformanceRunResult performanceResult)
    {
        var result = Substitute.For<ICompiledTestCaseResult>();
        result.Exception.Returns((Exception?)null);
        result.PerformanceRunResult.Returns(performanceResult);
        result.TestCaseId.Returns(new TestCaseId("TestClass.TestMethod()"));
        return result;
    }

    private IClassExecutionSummary CreateExecutionSummaryWithCompiledResult(ICompiledTestCaseResult compiledResult)
    {
        var summary = Substitute.For<IClassExecutionSummary>();
        summary.CompiledTestCaseResults.Returns(new List<ICompiledTestCaseResult> { compiledResult });
        return summary;
    }

    private PerformanceRunResult CreatePerformanceRunResult()
    {
        return new PerformanceRunResult(
            displayName: "TestMethod",
            mean: 150.5,
            stdDev: 25.3,
            variance: 640.09,
            median: 145.0,
            rawExecutionResults: new[] { 120.0, 130.0, 140.0, 150.0, 160.0, 170.0, 180.0 },
            sampleSize: 100,
            numWarmupIterations: 5,
            dataWithOutliersRemoved: new[] { 130.0, 140.0, 150.0, 160.0, 170.0 },
            upperOutliers: Array.Empty<double>(),
            lowerOutliers: Array.Empty<double>(),
            totalNumOutliers: 0);
    }

    private PerformanceRunResult CreatePerformanceRunResultWithUpperOutliers()
    {
        return new PerformanceRunResult(
            displayName: "TestMethod",
            mean: 150.5,
            stdDev: 25.3,
            variance: 640.09,
            median: 145.0,
            rawExecutionResults: new[] { 120.0, 130.0, 140.0, 150.0, 160.0, 170.0, 180.0, 250.0, 300.0 },
            sampleSize: 100,
            numWarmupIterations: 5,
            dataWithOutliersRemoved: new[] { 130.0, 140.0, 150.0, 160.0, 170.0 },
            upperOutliers: new[] { 250.0, 300.0 },
            lowerOutliers: Array.Empty<double>(),
            totalNumOutliers: 2);
    }

    private PerformanceRunResult CreatePerformanceRunResultWithLowerOutliers()
    {
        return new PerformanceRunResult(
            displayName: "TestMethod",
            mean: 150.5,
            stdDev: 25.3,
            variance: 640.09,
            median: 145.0,
            rawExecutionResults: new[] { 50.0, 60.0, 120.0, 130.0, 140.0, 150.0, 160.0, 170.0, 180.0 },
            sampleSize: 100,
            numWarmupIterations: 5,
            dataWithOutliersRemoved: new[] { 130.0, 140.0, 150.0, 160.0, 170.0 },
            upperOutliers: Array.Empty<double>(),
            lowerOutliers: new[] { 50.0, 60.0 },
            totalNumOutliers: 2);
    }

    private PerformanceRunResult CreateEmptyPerformanceRunResult()
    {
        return new PerformanceRunResult(
            displayName: "TestMethod",
            mean: 0,
            stdDev: 0,
            variance: 0,
            median: 0,
            rawExecutionResults: Array.Empty<double>(),
            sampleSize: 0,
            numWarmupIterations: 0,
            dataWithOutliersRemoved: Array.Empty<double>(),
            upperOutliers: Array.Empty<double>(),
            lowerOutliers: Array.Empty<double>(),
            totalNumOutliers: 0);
    }

    #endregion
}
