using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using Xunit;

namespace Tests.Library.Contracts.Public;

public class SailDiffResultMarkdownConverterTests
{
    private readonly ISailDiffUnifiedFormatter _mockUnifiedFormatter;
    private readonly SailDiffResultMarkdownConverter _converterWithFormatter;
    private readonly SailDiffResultMarkdownConverter _converterWithoutFormatter;

    public SailDiffResultMarkdownConverterTests()
    {
        _mockUnifiedFormatter = Substitute.For<ISailDiffUnifiedFormatter>();
        _converterWithFormatter = new SailDiffResultMarkdownConverter(_mockUnifiedFormatter);
        _converterWithoutFormatter = new SailDiffResultMarkdownConverter();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Default_SetsUnifiedFormatterToNull()
    {
        // Act
        var converter = new SailDiffResultMarkdownConverter();

        // Assert
        converter.ShouldNotBeNull();
        // We can't directly test the private field, but we can test the behavior
        var result = converter.ConvertToEnhancedMarkdownTable(new List<SailDiffResult>());
        result.ShouldBe("No SailDiff results available.");
    }

    [Fact]
    public void Constructor_WithUnifiedFormatter_SetsUnifiedFormatter()
    {
        // Arrange
        var mockFormatter = Substitute.For<ISailDiffUnifiedFormatter>();

        // Act
        var converter = new SailDiffResultMarkdownConverter(mockFormatter);

        // Assert
        converter.ShouldNotBeNull();
    }

    [Fact]
    public void Constructor_WithNullUnifiedFormatter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new SailDiffResultMarkdownConverter(null!));
    }

    #endregion

    #region ConvertToMarkdownTable Tests

    [Fact]
    public void ConvertToMarkdownTable_EmptyCollection_ReturnsEmptyString()
    {
        // Arrange
        var emptyResults = new List<SailDiffResult>();

        // Act
        var result = _converterWithoutFormatter.ConvertToMarkdownTable(emptyResults);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void ConvertToMarkdownTable_SingleResult_CreatesProperTable()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult> { CreateTestSailDiffResult() };

        // Act
        var result = _converterWithoutFormatter.ConvertToMarkdownTable(sailDiffResults);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("Display Name");
        result.ShouldContain("MeanBefore");
        result.ShouldContain("MeanAfter");
        result.ShouldContain("TestMethod");
    }

    [Fact]
    public void ConvertToMarkdownTable_MultipleResults_CreatesTableWithAllResults()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult>
        {
            CreateTestSailDiffResult("TestMethod1"),
            CreateTestSailDiffResult("TestMethod2")
        };

        // Act
        var result = _converterWithoutFormatter.ConvertToMarkdownTable(sailDiffResults);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("TestMethod1");
        result.ShouldContain("TestMethod2");
    }

    [Fact]
    public void ConvertToMarkdownTable_ResultsWithExceptions_IncludesExceptionColumn()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult>
        {
            CreateTestSailDiffResultWithException("Test exception message")
        };

        // Act
        var result = _converterWithoutFormatter.ConvertToMarkdownTable(sailDiffResults);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldContain("Exception");
        result.ShouldContain("Test exception message");
    }

    #endregion

    #region ConvertToEnhancedMarkdownTable Tests

    [Fact]
    public void ConvertToEnhancedMarkdownTable_EmptyCollection_ReturnsNoResultsMessage()
    {
        // Arrange
        var emptyResults = new List<SailDiffResult>();

        // Act
        var result = _converterWithoutFormatter.ConvertToEnhancedMarkdownTable(emptyResults);

        // Assert
        result.ShouldBe("No SailDiff results available.");
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTable_WithUnifiedFormatter_SingleResult_UsesUnifiedFormatter()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult> { CreateTestSailDiffResult() };
        var expectedOutput = new SailDiffFormattedOutput
        {
            FullOutput = "Mocked unified formatter output"
        };
        _mockUnifiedFormatter.Format(Arg.Any<SailDiffComparisonData>(), Arg.Any<OutputContext>())
            .Returns(expectedOutput);

        // Act
        var result = _converterWithFormatter.ConvertToEnhancedMarkdownTable(sailDiffResults);

        // Assert
        result.ShouldContain("Mocked unified formatter output");
        _mockUnifiedFormatter.Received(1).Format(Arg.Any<SailDiffComparisonData>(), Arg.Any<OutputContext>());
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTable_WithUnifiedFormatter_MultipleResults_UsesUnifiedFormatter()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult>
        {
            CreateTestSailDiffResult("TestMethod1"),
            CreateTestSailDiffResult("TestMethod2")
        };
        var expectedOutput = new SailDiffFormattedOutput
        {
            FullOutput = "Mocked multiple results output"
        };
        _mockUnifiedFormatter.FormatMultiple(Arg.Any<IEnumerable<SailDiffComparisonData>>(), Arg.Any<OutputContext>())
            .Returns(expectedOutput);

        // Act
        var result = _converterWithFormatter.ConvertToEnhancedMarkdownTable(sailDiffResults);

        // Assert
        result.ShouldContain("Mocked multiple results output");
        _mockUnifiedFormatter.Received(1).FormatMultiple(Arg.Any<IEnumerable<SailDiffComparisonData>>(), Arg.Any<OutputContext>());
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTable_WithoutUnifiedFormatter_UsesEnhancedLegacyFormat()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult> { CreateTestSailDiffResult() };

        // Act
        var result = _converterWithoutFormatter.ConvertToEnhancedMarkdownTable(sailDiffResults);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
        // Should contain both impact summary and table
        result.ShouldContain("TestMethod");
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTable_MarkdownContext_ProducesMarkdownOutput()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult> { CreateTestSailDiffResult() };

        // Act
        var result = _converterWithoutFormatter.ConvertToEnhancedMarkdownTable(sailDiffResults, OutputContext.Markdown);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTable_ConsoleContext_ProducesConsoleOutput()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult> { CreateTestSailDiffResult() };

        // Act
        var result = _converterWithoutFormatter.ConvertToEnhancedMarkdownTable(sailDiffResults, OutputContext.Console);

        // Assert
        result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
    }

    #endregion

    #region Edge Cases and Additional Scenarios

    [Fact]
    public void ConvertToMarkdownTable_ResultsWithDifferentSampleSizes_ThrowsException()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult>
        {
            CreateTestSailDiffResultWithSampleSize(50),
            CreateTestSailDiffResultWithSampleSize(100)
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _converterWithoutFormatter.ConvertToMarkdownTable(sailDiffResults));
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTable_WithMarkdownContext_IncludesMarkdownHeader()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult> { CreateTestSailDiffResult() };
        var expectedOutput = new SailDiffFormattedOutput
        {
            FullOutput = "## SailDiff Performance Analysis\n\nTest output"
        };
        _mockUnifiedFormatter.Format(Arg.Any<SailDiffComparisonData>(), OutputContext.Markdown)
            .Returns(expectedOutput);

        // Act
        var result = _converterWithFormatter.ConvertToEnhancedMarkdownTable(sailDiffResults, OutputContext.Markdown);

        // Assert
        result.ShouldContain("## SailDiff Performance Analysis");
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTable_SignificantImprovement_ShowsImprovedMessage()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult> { CreateTestSailDiffResultWithImprovement() };

        // Act
        var result = _converterWithoutFormatter.ConvertToEnhancedMarkdownTable(sailDiffResults, OutputContext.Markdown);

        // Assert
        (result.Contains("IMPROVED") || result.Contains("faster")).ShouldBeTrue();
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTable_SignificantRegression_ShowsRegressedMessage()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult> { CreateTestSailDiffResultWithRegression() };

        // Act
        var result = _converterWithoutFormatter.ConvertToEnhancedMarkdownTable(sailDiffResults, OutputContext.Markdown);

        // Assert
        (result.Contains("REGRESSED") || result.Contains("slower")).ShouldBeTrue();
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTable_NoSignificantChange_ShowsNoChangeMessage()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult> { CreateTestSailDiffResultWithNoChange() };

        // Act
        var result = _converterWithoutFormatter.ConvertToEnhancedMarkdownTable(sailDiffResults, OutputContext.Markdown);

        // Assert
        result.ShouldContain("NO CHANGE");
    }

    [Fact]
    public void ConvertToEnhancedMarkdownTable_ConsoleContext_UsesConsoleIcons()
    {
        // Arrange
        var sailDiffResults = new List<SailDiffResult> { CreateTestSailDiffResultWithImprovement() };

        // Act
        var result = _converterWithoutFormatter.ConvertToEnhancedMarkdownTable(sailDiffResults, OutputContext.Console);

        // Assert
        (result.Contains("[+]") || result.Contains("[-]") || result.Contains("[=]")).ShouldBeTrue();
    }

    #endregion

    #region Helper Methods

    private SailDiffResult CreateTestSailDiffResult(string displayName = "TestMethod")
    {
        var testCaseId = new TestCaseId(displayName);
        var statisticalTestResult = CreateTestStatisticalTestResult();
        var testResultWithOutlierAnalysis = new TestResultWithOutlierAnalysis(statisticalTestResult, null, null);
        return new SailDiffResult(testCaseId, testResultWithOutlierAnalysis);
    }

    private SailDiffResult CreateTestSailDiffResultWithException(string exceptionMessage)
    {
        var testCaseId = new TestCaseId("TestMethodWithException");
        var exception = new Exception(exceptionMessage);
        var testResultWithOutlierAnalysis = new TestResultWithOutlierAnalysis(exception);
        return new SailDiffResult(testCaseId, testResultWithOutlierAnalysis);
    }

    private SailDiffResult CreateTestSailDiffResultWithSampleSize(int sampleSize)
    {
        var testCaseId = new TestCaseId($"TestMethod_SampleSize_{sampleSize}");
        var statisticalTestResult = CreateTestStatisticalTestResultWithSampleSize(sampleSize);
        var testResultWithOutlierAnalysis = new TestResultWithOutlierAnalysis(statisticalTestResult, null, null);
        return new SailDiffResult(testCaseId, testResultWithOutlierAnalysis);
    }

    private SailDiffResult CreateTestSailDiffResultWithImprovement()
    {
        var testCaseId = new TestCaseId("ImprovedTestMethod");
        var statisticalTestResult = new StatisticalTestResult(
            meanBefore: 10.0,
            meanAfter: 5.0,  // 50% improvement
            medianBefore: 9.5,
            medianAfter: 4.5,
            testStatistic: 5.0,
            pValue: 0.001,  // Highly significant
            changeDescription: "Improved",
            sampleSizeBefore: 100,
            sampleSizeAfter: 100,
            rawDataBefore: new[] { 8.0, 9.0, 10.0, 11.0, 12.0 },
            rawDataAfter: new[] { 3.0, 4.0, 5.0, 6.0, 7.0 },
            additionalResults: new Dictionary<string, object>());
        var testResultWithOutlierAnalysis = new TestResultWithOutlierAnalysis(statisticalTestResult, null, null);
        return new SailDiffResult(testCaseId, testResultWithOutlierAnalysis);
    }

    private SailDiffResult CreateTestSailDiffResultWithRegression()
    {
        var testCaseId = new TestCaseId("RegressedTestMethod");
        var statisticalTestResult = new StatisticalTestResult(
            meanBefore: 5.0,
            meanAfter: 10.0,  // 100% regression
            medianBefore: 4.5,
            medianAfter: 9.5,
            testStatistic: -5.0,
            pValue: 0.001,  // Highly significant
            changeDescription: "Regressed",
            sampleSizeBefore: 100,
            sampleSizeAfter: 100,
            rawDataBefore: new[] { 3.0, 4.0, 5.0, 6.0, 7.0 },
            rawDataAfter: new[] { 8.0, 9.0, 10.0, 11.0, 12.0 },
            additionalResults: new Dictionary<string, object>());
        var testResultWithOutlierAnalysis = new TestResultWithOutlierAnalysis(statisticalTestResult, null, null);
        return new SailDiffResult(testCaseId, testResultWithOutlierAnalysis);
    }

    private SailDiffResult CreateTestSailDiffResultWithNoChange()
    {
        var testCaseId = new TestCaseId("NoChangeTestMethod");
        var statisticalTestResult = new StatisticalTestResult(
            meanBefore: 10.0,
            meanAfter: 10.1,  // Minimal change
            medianBefore: 9.5,
            medianAfter: 9.6,
            testStatistic: 0.1,
            pValue: 0.8,  // Not significant
            changeDescription: "No Change",
            sampleSizeBefore: 100,
            sampleSizeAfter: 100,
            rawDataBefore: new[] { 8.0, 9.0, 10.0, 11.0, 12.0 },
            rawDataAfter: new[] { 8.1, 9.1, 10.1, 11.1, 12.1 },
            additionalResults: new Dictionary<string, object>());
        var testResultWithOutlierAnalysis = new TestResultWithOutlierAnalysis(statisticalTestResult, null, null);
        return new SailDiffResult(testCaseId, testResultWithOutlierAnalysis);
    }

    private StatisticalTestResult CreateTestStatisticalTestResult()
    {
        return new StatisticalTestResult(
            meanBefore: 10.0,
            meanAfter: 8.0,
            medianBefore: 9.5,
            medianAfter: 7.5,
            testStatistic: 2.5,
            pValue: 0.03,
            changeDescription: "Improved",
            sampleSizeBefore: 100,
            sampleSizeAfter: 100,
            rawDataBefore: new[] { 8.0, 9.0, 10.0, 11.0, 12.0 },
            rawDataAfter: new[] { 6.0, 7.0, 8.0, 9.0, 10.0 },
            additionalResults: new Dictionary<string, object>());
    }

    private StatisticalTestResult CreateTestStatisticalTestResultWithSampleSize(int sampleSize)
    {
        return new StatisticalTestResult(
            meanBefore: 10.0,
            meanAfter: 8.0,
            medianBefore: 9.5,
            medianAfter: 7.5,
            testStatistic: 2.5,
            pValue: 0.03,
            changeDescription: "Improved",
            sampleSizeBefore: sampleSize,
            sampleSizeAfter: sampleSize,
            rawDataBefore: new[] { 8.0, 9.0, 10.0, 11.0, 12.0 },
            rawDataAfter: new[] { 6.0, 7.0, 8.0, 9.0, 10.0 },
            additionalResults: new Dictionary<string, object>());
    }

    #endregion
}
