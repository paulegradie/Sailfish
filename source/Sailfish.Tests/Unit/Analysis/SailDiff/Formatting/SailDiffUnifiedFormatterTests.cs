using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Tests.Unit.Analysis.SailDiff.Formatting;

[TestClass]
public class SailDiffUnifiedFormatterTests
{
    private ISailDiffUnifiedFormatter _formatter = null!;
    private SailDiffComparisonData _testData = null!;

    [TestInitialize]
    public void Setup()
    {
        _formatter = SailDiffUnifiedFormatterFactory.Create();
        _testData = CreateTestComparisonData();
    }

    [TestMethod]
    public void Format_IDE_Context_IncludesEmojisAndColors()
    {
        // Act
        var result = _formatter.Format(_testData, OutputContext.IDE);

        // Assert
        Assert.IsTrue(result.ImpactSummary.Contains("🔴"), "IDE output should contain emoji indicators");
        Assert.IsTrue(result.ImpactSummary.Contains("REGRESSED"), "IDE output should contain significance text");
        Assert.IsTrue(result.FullOutput.Contains("📊 PERFORMANCE COMPARISON"), "IDE output should contain formatted header");
        Assert.IsTrue(result.FullOutput.Contains("📋 DETAILED STATISTICS"), "IDE output should contain table header");
    }

    [TestMethod]
    public void Format_Markdown_Context_ProducesValidMarkdown()
    {
        // Act
        var result = _formatter.Format(_testData, OutputContext.Markdown);

        // Assert
        Assert.IsTrue(result.ImpactSummary.Contains("**"), "Markdown output should contain bold formatting");
        Assert.IsTrue(result.DetailedTable.Contains("|"), "Markdown output should contain table formatting");
        Assert.IsTrue(result.FullOutput.Contains("###"), "Markdown output should contain header formatting");
    }

    [TestMethod]
    public void Format_Console_Context_IsReadableInTerminal()
    {
        // Act
        var result = _formatter.Format(_testData, OutputContext.Console);

        // Assert
        Assert.IsFalse(result.ImpactSummary.Contains("🔴"), "Console output should not contain emojis");
        Assert.IsTrue(result.ImpactSummary.Contains("[-]"), "Console output should contain text indicators");
        Assert.IsTrue(result.FullOutput.Contains("PERFORMANCE COMPARISON"), "Console output should contain readable header");
        Assert.IsTrue(result.FullOutput.Contains("="), "Console output should contain separator lines");
    }

    [TestMethod]
    public void Format_CSV_Context_ProducesStructuredData()
    {
        // Act
        var result = _formatter.Format(_testData, OutputContext.CSV);

        // Assert
        Assert.IsTrue(result.DetailedTable.Contains(","), "CSV output should contain comma separators");
        Assert.IsTrue(result.DetailedTable.Contains("PrimaryMethod"), "CSV output should contain headers");
        Assert.IsFalse(result.DetailedTable.Contains("🔴"), "CSV output should not contain emojis");
    }

    [TestMethod]
    public void Format_AllContexts_ContainSameStatisticalData()
    {
        // Act
        var ideResult = _formatter.Format(_testData, OutputContext.IDE);
        var markdownResult = _formatter.Format(_testData, OutputContext.Markdown);
        var consoleResult = _formatter.Format(_testData, OutputContext.Console);

        // Assert
        Assert.AreEqual(ideResult.PercentageChange, markdownResult.PercentageChange, 0.1, 
            "All contexts should report same percentage change");
        Assert.AreEqual(ideResult.Significance, markdownResult.Significance, 
            "All contexts should report same significance");
        Assert.AreEqual(ideResult.IsStatisticallySignificant, consoleResult.IsStatisticallySignificant, 
            "All contexts should report same statistical significance");
    }

    [TestMethod]
    public void Format_SignificantChange_ShowsCorrectImpactSummary()
    {
        // Arrange
        var significantData = CreateSignificantRegressionData();

        // Act
        var result = _formatter.Format(significantData, OutputContext.IDE);

        // Assert
        Assert.AreEqual(ComparisonSignificance.Regressed, result.Significance);
        Assert.IsTrue(result.IsStatisticallySignificant);
        Assert.IsTrue(result.ImpactSummary.Contains("REGRESSED"));
        Assert.IsTrue(result.PercentageChange > 50, "Should show significant percentage change");
    }

    [TestMethod]
    public void Format_NoSignificantChange_ShowsNoChangeIndicator()
    {
        // Arrange
        var noChangeData = CreateNoSignificantChangeData();

        // Act
        var result = _formatter.Format(noChangeData, OutputContext.IDE);

        // Assert
        Assert.AreEqual(ComparisonSignificance.NoChange, result.Significance);
        Assert.IsFalse(result.IsStatisticallySignificant);
        Assert.IsTrue(result.ImpactSummary.Contains("NO CHANGE"));
        Assert.IsTrue(result.ImpactSummary.Contains("⚪"));
    }

    [TestMethod]
    public void Format_ImprovementChange_ShowsImprovedIndicator()
    {
        // Arrange
        var improvementData = CreateImprovementData();

        // Act
        var result = _formatter.Format(improvementData, OutputContext.IDE);

        // Assert
        Assert.AreEqual(ComparisonSignificance.Improved, result.Significance);
        Assert.IsTrue(result.IsStatisticallySignificant);
        Assert.IsTrue(result.ImpactSummary.Contains("IMPROVED"));
        Assert.IsTrue(result.ImpactSummary.Contains("🟢"));
    }

    [TestMethod]
    public void FormatMultiple_CombinesMultipleComparisons()
    {
        // Arrange
        var comparisons = new[]
        {
            CreateTestComparisonData("Method1", "Method2"),
            CreateTestComparisonData("Method1", "Method3"),
            CreateTestComparisonData("Method2", "Method3")
        };

        // Act
        var result = _formatter.FormatMultiple(comparisons, OutputContext.IDE);

        // Assert
        Assert.IsTrue(result.FullOutput.Contains("Method1"), "Should contain first method");
        Assert.IsTrue(result.FullOutput.Contains("Method2"), "Should contain second method");
        Assert.IsTrue(result.FullOutput.Contains("Method3"), "Should contain third method");
        Assert.IsTrue(result.ImpactSummary.Split('\n').Length >= 3, "Should contain multiple impact summaries");
    }

    [TestMethod]
    public void Format_NullData_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            _formatter.Format(null!, OutputContext.IDE));
    }

    [TestMethod]
    public void FormatMultiple_EmptyCollection_ReturnsNoComparisonsMessage()
    {
        // Act
        var result = _formatter.FormatMultiple(Array.Empty<SailDiffComparisonData>(), OutputContext.IDE);

        // Assert
        Assert.AreEqual("No comparisons available", result.ImpactSummary);
        Assert.AreEqual(ComparisonSignificance.NoChange, result.Significance);
        Assert.IsFalse(result.IsStatisticallySignificant);
    }

    private SailDiffComparisonData CreateTestComparisonData(string primaryMethod = "BubbleSort", string comparedMethod = "QuickSort")
    {
        return new SailDiffComparisonData
        {
            GroupName = "SortingAlgorithms",
            PrimaryMethodName = primaryMethod,
            ComparedMethodName = comparedMethod,
            Statistics = new StatisticalTestResult
            {
                MeanBefore = 1.909,
                MeanAfter = 0.006,
                MedianBefore = 1.850,
                MedianAfter = 0.005,
                PValue = 0.000001,
                ChangeDescription = "Regressed",
                SampleSizeBefore = 100,
                SampleSizeAfter = 100
            },
            Metadata = new ComparisonMetadata
            {
                SampleSize = 100,
                AlphaLevel = 0.05,
                TestType = "T-Test",
                OutliersRemoved = 3
            },
            IsPerspectiveBased = false
        };
    }

    private SailDiffComparisonData CreateSignificantRegressionData()
    {
        var data = CreateTestComparisonData();
        data.Statistics.MeanBefore = 0.5;
        data.Statistics.MeanAfter = 2.0;
        data.Statistics.PValue = 0.001;
        data.Statistics.ChangeDescription = "Regressed";
        return data;
    }

    private SailDiffComparisonData CreateNoSignificantChangeData()
    {
        var data = CreateTestComparisonData();
        data.Statistics.MeanBefore = 1.0;
        data.Statistics.MeanAfter = 1.02;
        data.Statistics.PValue = 0.234;
        data.Statistics.ChangeDescription = "No Change";
        return data;
    }

    private SailDiffComparisonData CreateImprovementData()
    {
        var data = CreateTestComparisonData();
        data.Statistics.MeanBefore = 2.0;
        data.Statistics.MeanAfter = 0.5;
        data.Statistics.PValue = 0.001;
        data.Statistics.ChangeDescription = "Improved";
        return data;
    }
}
