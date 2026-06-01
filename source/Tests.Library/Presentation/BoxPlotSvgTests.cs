using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Presentation;

public class BoxPlotSvgTests
{
    private static double[] Ramp(int n, double start = 1.0, double step = 1.0)
        => Enumerable.Range(0, n).Select(i => start + i * step).ToArray();

    #region BoxPlotSvgRenderer

    [Fact]
    public void RenderSvg_SingleSeries_EmitsSvgWithBoxMedianAndMean()
    {
        var series = BoxPlotData.FromSamples("Method", Ramp(20), mean: 10.5);
        var svg = BoxPlotSvgRenderer.RenderSvg(new[] { series }, DurationUnit.Milliseconds);

        svg.ShouldStartWith("<svg");
        svg.ShouldContain("class=\"bp-box\"");
        svg.ShouldContain("class=\"bp-median\"");
        svg.ShouldContain("class=\"bp-mean\"");
        svg.ShouldContain("</svg>");
    }

    [Fact]
    public void RenderSvg_TwoSeries_EmitsOneBoxPerSeries()
    {
        var series = new[]
        {
            BoxPlotData.FromSamples("A", Ramp(20, 1.0, 0.1), mean: 2.0),
            BoxPlotData.FromSamples("B", Ramp(20, 5.0, 0.2), mean: 7.0)
        };

        var svg = BoxPlotSvgRenderer.RenderSvg(series, DurationUnit.Milliseconds);

        CountOccurrences(svg, "class=\"bp-box\"").ShouldBe(2);
    }

    [Fact]
    public void RenderSvg_EscapesLabels()
    {
        var series = BoxPlotData.FromSamples("Evil<&>\"Name", Ramp(10), mean: 5.0);
        var svg = BoxPlotSvgRenderer.RenderSvg(new[] { series }, DurationUnit.Milliseconds);

        svg.ShouldContain("Evil&lt;&amp;&gt;&quot;Name");
        svg.ShouldNotContain("Evil<&>");
    }

    [Fact]
    public void RenderSvg_EmptySeries_ReturnsEmpty()
    {
        var empty = BoxPlotData.FromSamples("x", Array.Empty<double>(), mean: double.NaN);
        BoxPlotSvgRenderer.RenderSvg(new[] { empty }, DurationUnit.Milliseconds).ShouldBeEmpty();
    }

    #endregion

    #region PerformanceDistributionHtmlReportBuilder

    [Fact]
    public void Build_WithResults_ProducesHtmlDocumentWithSvgAndNames()
    {
        var summary = CreateSummary("MyBenchmarks",
            ("LoadUserById", "GroupA", Ramp(20, 1.0, 0.1)),
            ("LoadUserByEmail", "GroupA", Ramp(20, 2.0, 0.2)));

        var html = PerformanceDistributionHtmlReportBuilder.Build(new[] { summary });

        html.ShouldContain("<!doctype html>");
        html.ShouldContain("<svg");
        html.ShouldContain("MyBenchmarks");
        html.ShouldContain("GroupA");
        html.ShouldContain("LoadUserById");
        html.ShouldContain("LoadUserByEmail");
    }

    [Fact]
    public void Build_WithNoSummaries_ReturnsEmpty()
    {
        PerformanceDistributionHtmlReportBuilder.Build(Array.Empty<IClassExecutionSummary>()).ShouldBeEmpty();
    }

    [Fact]
    public void Build_WithNoPlottableData_ReturnsEmpty()
    {
        var summary = CreateSummary("Empty", ("M", "G", Array.Empty<double>()));
        PerformanceDistributionHtmlReportBuilder.Build(new[] { summary }).ShouldBeEmpty();
    }

    #endregion

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }

    private static IClassExecutionSummary CreateSummary(string className, params (string Display, string Group, double[] Data)[] cases)
    {
        var summary = Substitute.For<IClassExecutionSummary>();
        var testClass = Substitute.For<Type>();
        testClass.Name.Returns(className);
        summary.TestClass.Returns(testClass);

        var results = cases.Select(c =>
        {
            var perf = new PerformanceRunResult(
                c.Display, c.Data.Length > 0 ? c.Data.Average() : 0, 1.0, 1.0,
                c.Data.Length > 0 ? c.Data.Average() : 0,
                c.Data, c.Data.Length, 0, c.Data,
                Array.Empty<double>(), Array.Empty<double>(), 0);

            var compiled = Substitute.For<ICompiledTestCaseResult>();
            compiled.GroupingId.Returns(c.Group);
            compiled.TestCaseId.Returns(new TestCaseId(c.Display));
            compiled.PerformanceRunResult.Returns(perf);
            compiled.Exception.Returns((Exception?)null);
            return compiled;
        }).ToList();

        summary.CompiledTestCaseResults.Returns(results);
        return summary;
    }
}
