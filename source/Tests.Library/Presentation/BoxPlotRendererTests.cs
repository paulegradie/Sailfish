using System;
using System.Linq;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Presentation;

public class BoxPlotRendererTests
{
    private static double[] Ramp(int n, double start = 1.0, double step = 1.0)
        => Enumerable.Range(0, n).Select(i => start + i * step).ToArray();

    #region BoxPlotData

    [Fact]
    public void FromSamples_ComputesFiveNumberSummary()
    {
        var data = Ramp(9); // 1..9, median 5, quartiles 3 and 7
        var series = BoxPlotData.FromSamples("x", data, mean: 5.0);

        series.N.ShouldBe(9);
        series.Min.ShouldBe(1.0);
        series.Max.ShouldBe(9.0);
        series.Median.ShouldBe(5.0);
        series.Q1.ShouldBeLessThan(series.Median);
        series.Q3.ShouldBeGreaterThan(series.Median);
    }

    [Fact]
    public void FromSamples_FiltersNonFiniteValues()
    {
        var data = new[] { 1.0, double.NaN, 2.0, double.PositiveInfinity, 3.0 };
        var series = BoxPlotData.FromSamples("x", data, mean: 2.0);

        series.N.ShouldBe(3);
        series.Min.ShouldBe(1.0);
        series.Max.ShouldBe(3.0);
    }

    [Fact]
    public void FromSamples_EmptyInput_ProducesEmptySeries()
    {
        var series = BoxPlotData.FromSamples("x", Array.Empty<double>(), mean: double.NaN);
        series.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void FromSamples_SingleValue_HasNoSpread()
    {
        var series = BoxPlotData.FromSamples("x", new[] { 4.2 }, mean: 4.2);
        series.N.ShouldBe(1);
        series.HasNoSpread.ShouldBeTrue();
        series.Median.ShouldBe(4.2);
    }

    [Fact]
    public void FromSamples_RecomputesMeanWhenNonFinite()
    {
        var series = BoxPlotData.FromSamples("x", new[] { 2.0, 4.0 }, mean: double.NaN);
        series.Mean.ShouldBe(3.0);
    }

    #endregion

    #region AsciiBoxPlotRenderer

    [Fact]
    public void Render_SingleSeries_DrawsBoxMedianMeanAndLegend()
    {
        var series = BoxPlotData.FromSamples("Method", Ramp(20), mean: 10.5);
        var output = AsciiBoxPlotRenderer.Render(new[] { series }, DurationUnit.Milliseconds);

        output.ShouldContain("┿");   // median (heavy vertical crossing the centre line)
        output.ShouldContain("×");   // mean
        output.ShouldContain("┌");   // IQR box corner (hollow rectangle)
        output.ShouldContain("Time (ms)");
        output.ShouldContain("median"); // legend
        output.ShouldContain("n=20");
    }

    [Fact]
    public void Render_TwoSeries_ShareAxisAndAlignLanes()
    {
        var primary = BoxPlotData.FromSamples("Tracked", Ramp(30, 1.0, 0.1), mean: 2.5);
        var compared = BoxPlotData.FromSamples("Projected", Ramp(30, 5.0, 0.3), mean: 9.0);

        var output = AsciiBoxPlotRenderer.Render(new[] { primary, compared }, DurationUnit.Milliseconds);

        // Each series prints its sample count on its box-bottom row.
        var countLines = output.Split('\n').Where(l => l.Contains("n=")).ToList();
        countLines.Count.ShouldBe(2);

        // Exactly one shared axis ruler underlies both series (the only line carrying both a └ corner
        // and the ┬ ticks; box tops have ┬ but no └, box bottoms have └ but no ┬).
        output.Split('\n').Count(l => l.Contains('┬') && l.Contains('└')).ShouldBe(1);
    }

    [Fact]
    public void Render_AnnotatesRemovedOutlierCountWithoutStretchingAxis()
    {
        var series = BoxPlotData.FromSamples("x", Ramp(20), mean: 10.5, removedOutliers: new[] { 40.0, 50.0 });
        var output = AsciiBoxPlotRenderer.Render(new[] { series }, DurationUnit.Milliseconds);

        output.ShouldContain("(+2 outliers)");
        // The far outliers (40, 50) must not appear on the axis — it stays scaled to the cleaned 1..20.
        output.ShouldNotContain("50.0");
    }

    [Fact]
    public void Render_AllEqualValues_RendersSingleMarkerWithoutCrashing()
    {
        var series = BoxPlotData.FromSamples("flat", new[] { 3.0, 3.0, 3.0, 3.0 }, mean: 3.0);
        var output = AsciiBoxPlotRenderer.Render(new[] { series }, DurationUnit.Milliseconds);

        output.ShouldNotBeNullOrEmpty();
        output.ShouldContain("×");
    }

    [Fact]
    public void Render_EmptySeries_ReturnsEmptyString()
    {
        var empty = BoxPlotData.FromSamples("x", Array.Empty<double>(), mean: double.NaN);
        AsciiBoxPlotRenderer.Render(new[] { empty }, DurationUnit.Milliseconds).ShouldBeEmpty();
        AsciiBoxPlotRenderer.Render(Array.Empty<BoxPlotSeries>(), DurationUnit.Milliseconds).ShouldBeEmpty();
    }

    [Fact]
    public void Render_SubMillisecondData_AutoScalesAxisUnit()
    {
        // ~1–3 µs values (0.001–0.003 ms); the axis must render in microseconds, not "0.000 ms".
        var samples = Enumerable.Range(0, 30).Select(i => 0.001 + i * 0.00005).ToArray();
        var unit = DurationFormatter.SelectUnit(samples);
        var series = BoxPlotData.FromSamples("fast", samples, mean: 0.0017);

        var output = AsciiBoxPlotRenderer.Render(new[] { series }, unit);

        unit.ShouldBe(DurationUnit.Microseconds);
        output.ShouldContain("Time (µs)");
    }

    [Fact]
    public void Render_PadsAxisSoWhiskersDoNotPinToEdges()
    {
        var series = BoxPlotData.FromSamples("M", Ramp(40), mean: 20.5);
        var output = AsciiBoxPlotRenderer.Render(new[] { series }, DurationUnit.Milliseconds, width: 60);

        // The ruler is the LAST line carrying a └ (every series' box bottom also has └, but the ruler
        // comes after them; the tick-labels and legend below it have none). The whisker caps live on the
        // whisker row, which carries the median ┿ (the count is on the box-bottom row, not here).
        var rulerLine = output.Split('\n').Last(l => l.Contains('└'));
        var laneLine = output.Split('\n').First(l => l.Contains('┿'));

        // The axis ruler (└────┘) spans the full width; the data whisker caps (├ … ┤) must sit inside it.
        laneLine.IndexOf('├').ShouldBeGreaterThan(rulerLine.IndexOf('└'));
        laneLine.LastIndexOf('┤').ShouldBeLessThan(rulerLine.LastIndexOf('┘'));
    }

    [Fact]
    public void Render_RespectsRequestedWidth()
    {
        var series = BoxPlotData.FromSamples("x", Ramp(20), mean: 10.5);
        var output = AsciiBoxPlotRenderer.Render(new[] { series }, DurationUnit.Milliseconds, width: 30);

        output.ShouldContain("┿"); // a box with a median was drawn
        // With captions above the boxes there is no label column, so the axis ruler is exactly the
        // requested plot width (nothing widens the line).
        var rulerLine = output.Split('\n').Last(l => l.Contains('└'));
        rulerLine.Length.ShouldBe(30);
    }

    #endregion
}
