using System.Collections.Generic;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using Xunit;

namespace Sailfish.Tests.Unit.Analysis.SailDiff.Formatting;

public class ImpactSummaryFormatterTests
{
    private readonly ImpactSummaryFormatter _formatter;

    public ImpactSummaryFormatterTests()
    {
        _formatter = new ImpactSummaryFormatter();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Act & Assert
        _formatter.ShouldNotBeNull();
    }

    #endregion

    #region IDE Context Tests

    [Fact]
    public void CreateImpactSummary_IDE_WithImprovement_ShouldIncludeGreenIcon()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 2.0, meanAfter: 1.0, pValue: 0.001);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Ide);

        // Assert
        result.ShouldContain("🟢"); // Green icon for improvement
        result.ShouldContain("faster");
    }

    [Fact]
    public void CreateImpactSummary_IDE_WithRegression_ShouldIncludeRedIcon()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 1.0, meanAfter: 2.0, pValue: 0.001);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Ide);

        // Assert
        result.ShouldContain("🔴"); // Red icon for regression
        result.ShouldContain("slower");
    }

    [Fact]
    public void CreateImpactSummary_IDE_WithNoChange_ShouldIncludeWhiteIcon()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 1.0, meanAfter: 1.0, pValue: 0.5);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Ide);

        // Assert
        result.ShouldContain("⚪"); // White icon for no change
    }

    [Fact]
    public void CreateImpactSummary_IDE_ShouldIncludeMethodNames()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Ide);

        // Assert
        result.ShouldContain("PrimaryMethod");
        result.ShouldContain("ComparedMethod");
    }

    [Fact]
    public void CreateImpactSummary_IDE_ShouldIncludePercentageChange()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 1.0, meanAfter: 2.0, pValue: 0.001);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Ide);

        // Assert
        result.ShouldMatch(@"\d+\.\d+%"); // Should contain percentage
    }

    [Fact]
    public void CreateImpactSummary_IDE_WithSignificantChange_ShouldIncludePValue()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 1.0, meanAfter: 2.0, pValue: 0.001);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Ide);

        // Assert
        result.ShouldContain("P-Value:");
        result.ShouldContain("0.001000");
    }

    [Fact]
    public void CreateImpactSummary_IDE_WithSignificantChange_ShouldIncludeMeanTimes()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 1.5, meanAfter: 2.5, pValue: 0.001);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Ide);

        // Assert
        result.ShouldContain("Mean:");
        result.ShouldContain("1.500ms");
        result.ShouldContain("2.500ms");
    }

    #endregion

    #region Markdown Context Tests

    [Fact]
    public void CreateImpactSummary_Markdown_ShouldUseBoldFormatting()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Markdown);

        // Assert
        result.ShouldContain("**");
    }

    [Fact]
    public void CreateImpactSummary_Markdown_WithImprovement_ShouldIncludeGreenIcon()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 2.0, meanAfter: 1.0, pValue: 0.001);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Markdown);

        // Assert
        result.ShouldContain("🟢"); // Green icon for improvement
        result.ShouldContain("faster");
    }

    [Fact]
    public void CreateImpactSummary_Markdown_WithRegression_ShouldIncludeRedIcon()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 1.0, meanAfter: 2.0, pValue: 0.001);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Markdown);

        // Assert
        result.ShouldContain("🔴"); // Red icon for regression
        result.ShouldContain("slower");
    }

    [Fact]
    public void CreateImpactSummary_Markdown_ShouldIncludeMethodNames()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Markdown);

        // Assert
        result.ShouldContain("PrimaryMethod");
        result.ShouldContain("ComparedMethod");
    }

    #endregion

    #region Console Context Tests

    [Fact]
    public void CreateImpactSummary_Console_ShouldNotIncludeEmojis()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Console);

        // Assert
        result.ShouldNotContain("🟢");
        result.ShouldNotContain("🔴");
        result.ShouldNotContain("🟡");
    }

    [Fact]
    public void CreateImpactSummary_Console_WithImprovement_ShouldIncludePlusIndicator()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 2.0, meanAfter: 1.0, pValue: 0.001);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Console);

        // Assert
        result.ShouldContain("[+]"); // Plus indicator for improvement
        result.ShouldContain("faster");
    }

    [Fact]
    public void CreateImpactSummary_Console_WithRegression_ShouldIncludeMinusIndicator()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 1.0, meanAfter: 2.0, pValue: 0.001);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Console);

        // Assert
        result.ShouldContain("[-]"); // Minus indicator for regression
        result.ShouldContain("slower");
    }

    [Fact]
    public void CreateImpactSummary_Console_WithNoChange_ShouldIncludeEqualIndicator()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 1.0, meanAfter: 1.0, pValue: 0.5);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Console);

        // Assert
        result.ShouldContain("[=]"); // Equal indicator for no change
    }

    #endregion

    #region CSV Context Tests

    [Fact]
    public void CreateImpactSummary_CSV_ShouldBeCommaDelimited()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Csv);

        // Assert
        result.Split(',').Length.ShouldBeGreaterThan(5); // Should have multiple comma-separated values
    }

    [Fact]
    public void CreateImpactSummary_CSV_ShouldIncludeMethodNames()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Csv);

        // Assert
        result.ShouldContain("PrimaryMethod");
        result.ShouldContain("ComparedMethod");
    }

    [Fact]
    public void CreateImpactSummary_CSV_ShouldIncludePercentageChange()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 1.0, meanAfter: 2.0, pValue: 0.001);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Csv);

        // Assert
        result.ShouldMatch(@"\d+\.\d+%");
    }

    [Fact]
    public void CreateImpactSummary_CSV_ShouldIncludePValue()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 1.0, meanAfter: 2.0, pValue: 0.001234);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Csv);

        // Assert
        result.ShouldContain("0.001234");
    }

    #endregion

    #region Perspective-Based Comparison Tests

    [Fact]
    public void CreateImpactSummary_WithPerspectiveBasedComparison_ShouldSwapTimesCorrectly()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 1.0, meanAfter: 2.0, pValue: 0.001);
        data.IsPerspectiveBased = true;
        data.PerspectiveMethodName = data.ComparedMethodName;

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Ide);

        // Assert
        result.ShouldNotBeEmpty();
        // When perspective is the compared method, times should be swapped
    }

    #endregion

    #region Unknown Context Tests

    [Fact]
    public void CreateImpactSummary_WithUnknownContext_ShouldDefaultToConsole()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateImpactSummary(data, (OutputContext)999);

        // Assert
        result.ShouldContain("IMPACT:");
        result.ShouldNotContain("🟢"); // Should not have emoji
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CreateImpactSummary_WithZeroPrimaryTime_ShouldHandleGracefully()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 0.0, meanAfter: 1.0, pValue: 0.001);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Ide);

        // Assert
        result.ShouldNotBeEmpty();
        result.ShouldContain("IMPACT:");
    }

    [Fact]
    public void CreateImpactSummary_WithVerySmallPValue_ShouldFormatCorrectly()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 1.0, meanAfter: 2.0, pValue: 0.000001);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Ide);

        // Assert
        result.ShouldContain("0.000001");
    }

    [Fact]
    public void CreateImpactSummary_WithLargePValue_ShouldIndicateNoChange()
    {
        // Arrange
        var data = CreateSampleComparisonData(meanBefore: 1.0, meanAfter: 1.5, pValue: 0.9);

        // Act
        var result = _formatter.CreateImpactSummary(data, OutputContext.Ide);

        // Assert
        result.ShouldContain("⚪"); // White for no significant change
    }

    #endregion

    #region Helper Methods

    private SailDiffComparisonData CreateSampleComparisonData(
        double meanBefore = 1.5,
        double meanAfter = 2.5,
        double pValue = 0.001234,
        string primaryMethod = "PrimaryMethod",
        string comparedMethod = "ComparedMethod")
    {
        return new SailDiffComparisonData
        {
            GroupName = "TestGroup",
            PrimaryMethodName = primaryMethod,
            ComparedMethodName = comparedMethod,
            Statistics = new StatisticalTestResult(
                meanBefore: meanBefore,
                meanAfter: meanAfter,
                medianBefore: meanBefore * 0.9,
                medianAfter: meanAfter * 0.9,
                testStatistic: 3.5,
                pValue: pValue,
                changeDescription: meanAfter > meanBefore ? "Regressed" : "Improved",
                sampleSizeBefore: 100,
                sampleSizeAfter: 100,
                rawDataBefore: [meanBefore - 0.5, meanBefore, meanBefore + 0.5],
                rawDataAfter: [meanAfter - 0.5, meanAfter, meanAfter + 0.5],
                additionalResults: new Dictionary<string, object>()),
            Metadata = new ComparisonMetadata
            {
                SampleSize = 100,
                AlphaLevel = 0.05,
                TestType = "T-Test",
                OutliersRemoved = 2
            },
            IsPerspectiveBased = false,
            PerspectiveMethodName = null
        };
    }

    #endregion
}

