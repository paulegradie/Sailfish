using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Formatting;

public class DetailedTableFormatterTests
{
    private readonly DetailedTableFormatter _formatter;

    public DetailedTableFormatterTests()
    {
        _formatter = new DetailedTableFormatter();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Act & Assert
        _formatter.ShouldNotBeNull();
    }

    #endregion

    #region Single Comparison Tests

    [Fact]
    public void CreateDetailedTable_WithSingleComparison_IDE_ShouldIncludeEmojiHeader()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateDetailedTable(data, OutputContext.Ide);

        // Assert
        result.ShouldContain("📋 DETAILED STATISTICS:");
    }

    [Fact]
    public void CreateDetailedTable_WithSingleComparison_IDE_ShouldIncludeMetrics()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateDetailedTable(data, OutputContext.Ide);

        // Assert
        result.ShouldContain("Mean");
        result.ShouldContain("Median");
        result.ShouldContain("P-Value");
    }

    [Fact]
    public void CreateDetailedTable_WithSingleComparison_Markdown_ShouldIncludeMarkdownTable()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateDetailedTable(data, OutputContext.Markdown);

        // Assert
        result.ShouldContain("| Metric |");
        result.ShouldContain("|--------|");
    }

    [Fact]
    public void CreateDetailedTable_WithSingleComparison_Console_ShouldIncludePlainTextHeader()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateDetailedTable(data, OutputContext.Console);

        // Assert
        result.ShouldContain("DETAILED STATISTICS:");
    }

    [Fact]
    public void CreateDetailedTable_WithSingleComparison_CSV_ShouldIncludeCSVHeaders()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateDetailedTable(data, OutputContext.Csv);

        // Assert
        result.ShouldContain("PrimaryMethod,ComparedMethod,PrimaryMean,ComparedMean,PrimaryMedian,ComparedMedian,PValue,ChangeDescription,SampleSize");
    }

    #endregion

    #region Multiple Comparisons Tests

    [Fact]
    public void CreateDetailedTable_WithMultipleComparisons_ShouldIncludeAllComparisons()
    {
        // Arrange
        var comparisons = new[]
        {
            CreateSampleComparisonData("Method1", "Method2"),
            CreateSampleComparisonData("Method3", "Method4")
        };

        // Act
        var result = _formatter.CreateDetailedTable(comparisons, OutputContext.Ide);

        // Assert
        result.ShouldContain("Method1");
        result.ShouldContain("Method2");
        result.ShouldContain("Method3");
        result.ShouldContain("Method4");
    }

    [Fact]
    public void CreateDetailedTable_WithMultipleComparisons_ShouldIncludeComparisonHeaders()
    {
        // Arrange
        var comparisons = new[]
        {
            CreateSampleComparisonData("Method1", "Method2"),
            CreateSampleComparisonData("Method3", "Method4")
        };

        // Act
        var result = _formatter.CreateDetailedTable(comparisons, OutputContext.Ide);

        // Assert
        result.ShouldContain("Comparing: Method2 vs baseline Method1");
        result.ShouldContain("Comparing: Method4 vs baseline Method3");
    }

    #endregion

    #region Empty Data Tests

    [Fact]
    public void CreateDetailedTable_WithEmptyCollection_ShouldReturnEmptyString()
    {
        // Arrange
        var emptyComparisons = Enumerable.Empty<SailDiffComparisonData>();

        // Act
        var result = _formatter.CreateDetailedTable(emptyComparisons, OutputContext.Ide);

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region Perspective-Based Comparison Tests

    [Fact]
    public void CreateDetailedTable_WithPerspectiveBasedComparison_ShouldSwapTimesCorrectly()
    {
        // Arrange
        var data = CreateSampleComparisonData();
        data.IsPerspectiveBased = true;
        data.PerspectiveMethodName = data.ComparedMethodName;

        // Act
        var result = _formatter.CreateDetailedTable(data, OutputContext.Ide);

        // Assert
        result.ShouldNotBeEmpty();
        // When perspective is the compared method, times should be swapped
        result.ShouldContain("ms");
    }

    #endregion

    #region Unknown Context Tests

    [Fact]
    public void CreateDetailedTable_WithUnknownContext_ShouldDefaultToConsole()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateDetailedTable(data, (OutputContext)999);

        // Assert
        result.ShouldContain("DETAILED STATISTICS:");
        result.ShouldNotContain("📋"); // Should not have emoji
    }

    #endregion

    #region Format Validation Tests

    [Fact]
    public void CreateDetailedTable_IDE_ShouldFormatPercentageChange()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateDetailedTable(data, OutputContext.Ide);

        // Assert
        result.ShouldMatch(@"[+-]\d+\.\d+%"); // Should contain percentage with sign
    }

    [Fact]
    public void CreateDetailedTable_IDE_ShouldFormatPValue()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateDetailedTable(data, OutputContext.Ide);

        // Assert
        result.ShouldMatch(@"\d+\.\d{6}"); // Should contain P-value with 6 decimal places
    }

    [Fact]
    public void CreateDetailedTable_Markdown_ShouldHaveProperTableStructure()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateDetailedTable(data, OutputContext.Markdown);

        // Assert
        var lines = result.Split('\n');
        lines.Count(l => l.Contains("|")).ShouldBeGreaterThan(2); // Should have multiple table rows
    }

    [Fact]
    public void CreateDetailedTable_CSV_ShouldHaveCommaDelimitedValues()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateDetailedTable(data, OutputContext.Csv);

        // Assert
        var lines = result.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        lines.ShouldAllBe(line => line.Contains(",")); // All lines should have commas
    }

    #endregion

    #region Sample Size Display Tests

    [Fact]
    public void CreateDetailedTable_IDE_ShouldDisplaySampleSize()
    {
        // Arrange
        var data = CreateSampleComparisonData();

        // Act
        var result = _formatter.CreateDetailedTable(data, OutputContext.Ide);

        // Assert
        result.ShouldContain("Sample Size");
        result.ShouldContain("100"); // From sample data
    }

    #endregion

    #region Helper Methods

    [Fact]
    public void CreateDetailedTable_WithSubMicrosecondTimings_RecoversPrecisionAndAutoScalesUnit()
    {
        // The scalar means are pre-rounded to 3 ms-decimals (0.001) — identical, which would make
        // %Δ a degenerate 0.0% and the cells "0.000ms". The raw samples (~1.1 µs vs ~1.4 µs) carry
        // the real values, so the formatter must recompute from them and auto-scale to microseconds.
        var data = new SailDiffComparisonData
        {
            GroupName = "FastGroup",
            PrimaryMethodName = "Primary",
            ComparedMethodName = "Compared",
            Statistics = new StatisticalTestResult(
                meanBefore: 0.001,
                meanAfter: 0.001,
                medianBefore: 0.001,
                medianAfter: 0.001,
                testStatistic: 3.5,
                pValue: 0.001234,
                changeDescription: "Regressed",
                sampleSizeBefore: 100,
                sampleSizeAfter: 100,
                rawDataBefore: [0.0010, 0.0011, 0.0012],
                rawDataAfter: [0.0013, 0.0014, 0.0015],
                additionalResults: new Dictionary<string, object>()),
            Metadata = new ComparisonMetadata { SampleSize = 100, AlphaLevel = 0.05, TestType = "T-Test" },
            IsPerspectiveBased = false
        };

        var ide = _formatter.CreateDetailedTable(data, OutputContext.Ide);
        var markdown = _formatter.CreateDetailedTable(data, OutputContext.Markdown);

        ide.ShouldContain("Mean (µs)");
        ide.ShouldContain("1.100");          // ~1.1 µs, not 0.000ms
        ide.ShouldContain("1.400");          // ~1.4 µs
        ide.ShouldContain("+27.3%");         // non-degenerate %Δ recovered from raw data
        ide.ShouldNotContain("0.000ms");
        markdown.ShouldContain("Mean (µs)");
    }

    private SailDiffComparisonData CreateSampleComparisonData(
        string primaryMethod = "PrimaryMethod",
        string comparedMethod = "ComparedMethod")
    {
        return new SailDiffComparisonData
        {
            GroupName = "TestGroup",
            PrimaryMethodName = primaryMethod,
            ComparedMethodName = comparedMethod,
            Statistics = new StatisticalTestResult(
                meanBefore: 1.5,
                meanAfter: 2.5,
                medianBefore: 1.4,
                medianAfter: 2.4,
                testStatistic: 3.5,
                pValue: 0.001234,
                changeDescription: "Regressed",
                sampleSizeBefore: 100,
                sampleSizeAfter: 100,
                rawDataBefore: [1.0, 1.5, 2.0],
                rawDataAfter: [2.0, 2.5, 3.0],
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

