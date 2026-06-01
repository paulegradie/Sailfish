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

        output.ShouldContain("┃");   // median
        output.ShouldContain("◆");   // mean
        output.ShouldContain("▓");   // IQR box
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

        var laneLines = output
            .Split('\n')
            .Where(l => l.Contains("n=")) // series lanes are suffixed with the sample count
            .ToList();

        laneLines.Count.ShouldBe(2);
        // Lanes share the same total width (labels are padded to equal length).
        laneLines.Select(l => l.Length).Distinct().Count().ShouldBe(1);
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
        output.ShouldContain("◆");
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
    public void Render_RespectsRequestedWidth()
    {
        var series = BoxPlotData.FromSamples("x", Ramp(20), mean: 10.5);
        var output = AsciiBoxPlotRenderer.Render(new[] { series }, DurationUnit.Milliseconds, width: 30);

        var laneLine = output.Split('\n').First(l => l.Contains("n=20"));
        // label("x"=>1) + 2 spaces + 30 lane + "  n=20"
        laneLine.ShouldContain(new string('▓', 1)); // some box drawn
        laneLine.Length.ShouldBeGreaterThan(30);
    }

    #endregion
}
