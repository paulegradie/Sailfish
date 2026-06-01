using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff.Formatting;

public class DistributionPlotFormatterTests
{
    private readonly DistributionPlotFormatter _formatter = new();

    private static SailDiffComparisonData CreateComparison(
        string primary = "Primary",
        string compared = "Compared",
        bool includePlot = true)
    {
        return new SailDiffComparisonData
        {
            GroupName = "Grp",
            PrimaryMethodName = primary,
            ComparedMethodName = compared,
            Statistics = new StatisticalTestResult(
                meanBefore: 2.0, meanAfter: 4.0,
                medianBefore: 2.0, medianAfter: 4.0,
                testStatistic: 3.0, pValue: 0.001,
                changeDescription: "Regressed",
                sampleSizeBefore: 20, sampleSizeAfter: 20,
                rawDataBefore: Enumerable.Range(0, 20).Select(i => 1.0 + i * 0.1).ToArray(),
                rawDataAfter: Enumerable.Range(0, 20).Select(i => 3.0 + i * 0.2).ToArray(),
                additionalResults: new Dictionary<string, object>()),
            Metadata = new ComparisonMetadata { SampleSize = 20, AlphaLevel = 0.05, IncludeDistributionPlot = includePlot },
            IsPerspectiveBased = false
        };
    }

    [Fact]
    public void CreatePlot_Ide_IncludesPlotHeaderHistogramAndBothMethods()
    {
        var output = _formatter.CreatePlot(CreateComparison(), OutputContext.Ide);

        output.ShouldContain("📊 DISTRIBUTION");
        output.ShouldContain("█");                 // histogram block glyph
        output.ShouldContain("count per bin");      // legend
        output.ShouldContain("Primary");
        output.ShouldContain("Compared");
    }

    [Fact]
    public void CreatePlot_Markdown_WrapsInFencedCodeBlock()
    {
        var output = _formatter.CreatePlot(CreateComparison(), OutputContext.Markdown);

        output.ShouldContain("**Distribution**");
        output.ShouldContain("```text");
        output.TrimEnd().ShouldEndWith("```");
    }

    [Fact]
    public void CreatePlot_Csv_ReturnsEmpty()
    {
        _formatter.CreatePlot(CreateComparison(), OutputContext.Csv).ShouldBeEmpty();
    }

    [Fact]
    public void CreatePlot_WhenMetadataDisablesPlot_ReturnsEmpty()
    {
        _formatter.CreatePlot(CreateComparison(includePlot: false), OutputContext.Ide).ShouldBeEmpty();
    }

    [Fact]
    public void CreatePlot_WhenRunSettingsDisablePlots_ReturnsEmpty()
    {
        var runSettings = Substitute.For<IRunSettings>();
        runSettings.EnableDistributionPlots.Returns(false);
        var formatter = new DistributionPlotFormatter(runSettings);

        formatter.CreatePlot(CreateComparison(), OutputContext.Ide).ShouldBeEmpty();
    }

    [Fact]
    public void CreatePlot_WhenStatisticsFailed_ReturnsEmpty()
    {
        var data = CreateComparison();
        data.Statistics.Failed = true;

        _formatter.CreatePlot(data, OutputContext.Ide).ShouldBeEmpty();
    }

    [Fact]
    public void CreatePlot_MultipleComparisons_DrawsEachDistinctMethodOnce()
    {
        // Two comparisons sharing the same primary ("Tracked") => Tracked appears once, plus two others.
        var comparisons = new[]
        {
            CreateComparison("Tracked", "NoTracking"),
            CreateComparison("Tracked", "Projected")
        };

        var output = _formatter.CreatePlot(comparisons, OutputContext.Ide);

        output.ShouldContain("Tracked");
        output.ShouldContain("NoTracking");
        output.ShouldContain("Projected");
        // "Tracked" is shared across both comparisons but de-duplicated to a single summary row.
        output.Split('\n').Count(l => l.Contains("Tracked") && l.Contains("n=")).ShouldBe(1);
    }

    [Fact]
    public void UnifiedFormatter_Format_EmbedsDistributionPlotInFullOutput()
    {
        var unified = SailDiffUnifiedFormatterFactory.Create();

        var result = unified.Format(CreateComparison(), OutputContext.Ide);

        result.DistributionPlot.ShouldContain("📊 DISTRIBUTION");
        result.FullOutput.ShouldContain("📊 DISTRIBUTION");
        result.FullOutput.ShouldContain("📊 PERFORMANCE COMPARISON"); // still has the existing block
    }
}
