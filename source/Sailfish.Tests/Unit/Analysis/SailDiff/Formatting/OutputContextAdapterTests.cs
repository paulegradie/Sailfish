using System;
using Sailfish.Analysis.SailDiff.Formatting;
using Shouldly;
using Xunit;

namespace Sailfish.Tests.Unit.Analysis.SailDiff.Formatting;

/// <summary>
/// Comprehensive unit tests for OutputContextAdapter.
/// Tests the adaptation of formatted SailDiff output to different contexts 
/// (IDE, Markdown, Console, CSV) with proper formatting and layout.
/// </summary>
public class OutputContextAdapterTests
{
    private readonly OutputContextAdapter _adapter;
    private const string SampleImpactSummary = "Performance improved by 25% (statistically significant)";
    private const string SampleDetailedTable = "| Method | Before | After | Change |\n|--------|--------|-------|--------|\n| Test1  | 100ms  | 75ms  | -25%   |";

    public OutputContextAdapterTests()
    {
        _adapter = new OutputContextAdapter();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        // Act & Assert
        _adapter.ShouldNotBeNull();
    }

    #endregion

    #region IDE Context Tests

    [Fact]
    public void AdaptToContext_IDE_ShouldIncludeEmojiHeader()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.IDE);

        // Assert
        result.ShouldContain("📊 PERFORMANCE COMPARISON");
    }

    [Fact]
    public void AdaptToContext_IDE_ShouldIncludeSeparators()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.IDE);

        // Assert
        result.ShouldContain(new string('=', 50));
    }

    [Fact]
    public void AdaptToContext_IDE_WithGroupName_ShouldIncludeGroupName()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.IDE, "TestGroup");

        // Assert
        result.ShouldContain("Group: TestGroup");
    }

    [Fact]
    public void AdaptToContext_IDE_ShouldIncludeImpactSummary()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.IDE);

        // Assert
        result.ShouldContain(SampleImpactSummary);
    }

    [Fact]
    public void AdaptToContext_IDE_ShouldIncludeDetailedTable()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.IDE);

        // Assert
        result.ShouldContain(SampleDetailedTable);
    }

    [Fact]
    public void AdaptToContext_IDE_WithEmptyDetailedTable_ShouldNotIncludeTable()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, "", OutputContext.IDE);

        // Assert
        result.ShouldNotContain("| Method |");
        result.ShouldContain(SampleImpactSummary);
    }

    #endregion

    #region Markdown Context Tests

    [Fact]
    public void AdaptToContext_Markdown_ShouldIncludeMarkdownHeader()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.Markdown);

        // Assert
        result.ShouldContain("### Performance Comparison");
    }

    [Fact]
    public void AdaptToContext_Markdown_WithGroupName_ShouldIncludeGroupInHeader()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.Markdown, "TestGroup");

        // Assert
        result.ShouldContain("### TestGroup Performance Comparison");
    }

    [Fact]
    public void AdaptToContext_Markdown_ShouldIncludeImpactSummary()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.Markdown);

        // Assert
        result.ShouldContain(SampleImpactSummary);
    }

    [Fact]
    public void AdaptToContext_Markdown_ShouldIncludeDetailedTable()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.Markdown);

        // Assert
        result.ShouldContain(SampleDetailedTable);
    }

    [Fact]
    public void AdaptToContext_Markdown_ShouldNotIncludeEmojis()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.Markdown);

        // Assert
        result.ShouldNotContain("📊");
    }

    #endregion

    #region Console Context Tests

    [Fact]
    public void AdaptToContext_Console_ShouldIncludePlainTextHeader()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.Console);

        // Assert
        result.ShouldContain("PERFORMANCE COMPARISON");
        result.ShouldNotContain("📊");
        result.ShouldNotContain("###");
    }

    [Fact]
    public void AdaptToContext_Console_ShouldIncludeSeparators()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.Console);

        // Assert
        result.ShouldContain(new string('=', 60));
    }

    [Fact]
    public void AdaptToContext_Console_WithGroupName_ShouldIncludeGroupName()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.Console, "TestGroup");

        // Assert
        result.ShouldContain("Group: TestGroup");
    }

    [Fact]
    public void AdaptToContext_Console_ShouldIncludeImpactSummary()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.Console);

        // Assert
        result.ShouldContain(SampleImpactSummary);
    }

    [Fact]
    public void AdaptToContext_Console_ShouldIncludeDetailedTable()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.Console);

        // Assert
        result.ShouldContain(SampleDetailedTable);
    }

    #endregion

    #region CSV Context Tests

    [Fact]
    public void AdaptToContext_CSV_WithGroupName_ShouldIncludeMetadataHeader()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.CSV, "TestGroup");

        // Assert
        result.ShouldContain("# Group: TestGroup");
        result.ShouldContain("# Generated:");
    }

    [Fact]
    public void AdaptToContext_CSV_WithDetailedTable_ShouldReturnTable()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.CSV);

        // Assert
        result.ShouldContain(SampleDetailedTable);
    }

    [Fact]
    public void AdaptToContext_CSV_WithoutDetailedTable_ShouldReturnSummaryAsCSV()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, "", OutputContext.CSV);

        // Assert
        result.ShouldContain("Summary");
        result.ShouldContain($"\"{SampleImpactSummary}\"");
    }

    [Fact]
    public void AdaptToContext_CSV_WithoutGroupName_ShouldNotIncludeMetadata()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, OutputContext.CSV);

        // Assert
        result.ShouldNotContain("# Group:");
        result.ShouldNotContain("# Generated:");
    }

    #endregion

    #region Unknown Context Tests

    [Fact]
    public void AdaptToContext_UnknownContext_ShouldDefaultToConsole()
    {
        // Act
        var result = _adapter.AdaptToContext(SampleImpactSummary, SampleDetailedTable, (OutputContext)999);

        // Assert
        result.ShouldContain("PERFORMANCE COMPARISON");
        result.ShouldContain(new string('=', 60));
    }

    #endregion

    #region Extension Methods Tests

    [Fact]
    public void SupportsRichFormatting_IDE_ShouldReturnTrue()
    {
        // Act & Assert
        OutputContext.IDE.SupportsRichFormatting().ShouldBeTrue();
    }

    [Fact]
    public void SupportsRichFormatting_Markdown_ShouldReturnTrue()
    {
        // Act & Assert
        OutputContext.Markdown.SupportsRichFormatting().ShouldBeTrue();
    }

    [Fact]
    public void SupportsRichFormatting_Console_ShouldReturnFalse()
    {
        // Act & Assert
        OutputContext.Console.SupportsRichFormatting().ShouldBeFalse();
    }

    [Fact]
    public void SupportsRichFormatting_CSV_ShouldReturnFalse()
    {
        // Act & Assert
        OutputContext.CSV.SupportsRichFormatting().ShouldBeFalse();
    }

    [Fact]
    public void SupportsTableFormatting_AllContexts_ShouldReturnTrue()
    {
        // Act & Assert
        OutputContext.IDE.SupportsTableFormatting().ShouldBeTrue();
        OutputContext.Markdown.SupportsTableFormatting().ShouldBeTrue();
        OutputContext.Console.SupportsTableFormatting().ShouldBeTrue();
        OutputContext.CSV.SupportsTableFormatting().ShouldBeTrue();
    }

    [Fact]
    public void GetLineSeparator_CSV_ShouldReturnNewline()
    {
        // Act & Assert
        OutputContext.CSV.GetLineSeparator().ShouldBe("\n");
    }

    [Fact]
    public void GetLineSeparator_OtherContexts_ShouldReturnEnvironmentNewLine()
    {
        // Act & Assert
        OutputContext.IDE.GetLineSeparator().ShouldBe(Environment.NewLine);
        OutputContext.Markdown.GetLineSeparator().ShouldBe(Environment.NewLine);
        OutputContext.Console.GetLineSeparator().ShouldBe(Environment.NewLine);
    }

    [Fact]
    public void GetMaxLineLength_ShouldReturnCorrectValues()
    {
        // Act & Assert
        OutputContext.IDE.GetMaxLineLength().ShouldBe(120);
        OutputContext.Markdown.GetMaxLineLength().ShouldBe(100);
        OutputContext.Console.GetMaxLineLength().ShouldBe(80);
        OutputContext.CSV.GetMaxLineLength().ShouldBe(int.MaxValue);
    }

    #endregion
}
