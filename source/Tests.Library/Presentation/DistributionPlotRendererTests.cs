using System;
using System.Linq;
using Sailfish.Presentation;
using Shouldly;
using Xunit;

namespace Tests.Library.Presentation;

public class DistributionPlotRendererTests
{
    private static DistributionPlotRenderer.Series Series()
    {
        var samples = Enumerable.Range(0, 200).Select(i => 51.0 + 0.2 * Math.Sin(i * 2.1)).ToArray();
        return new DistributionPlotRenderer.Series("method", samples, double.NaN, double.NaN, new[] { 60.0, 61.0 });
    }

    [Fact]
    public void Render_HistogramStyle_UsesHistogramGlyphs()
    {
        var output = DistributionPlotRenderer.Render(new[] { Series() }, DurationUnit.Milliseconds, DistributionPlotStyle.Histogram);

        output.ShouldContain("count per bin");                              // histogram legend
        output.IndexOfAny("▁▂▃▄▅▆▇█".ToCharArray()).ShouldBeGreaterThanOrEqualTo(0);
        output.ShouldNotContain("IQR box");                                 // not the box-plot legend
    }

    [Fact]
    public void Render_BoxPlotStyle_UsesBoxPlotGlyphs()
    {
        var output = DistributionPlotRenderer.Render(new[] { Series() }, DurationUnit.Milliseconds, DistributionPlotStyle.BoxPlot);

        output.ShouldContain("IQR box");          // box-plot legend
        output.ShouldContain("▓");                 // IQR box fill
        output.ShouldNotContain("count per bin");  // not the histogram legend
    }

    [Fact]
    public void Render_BoxPlotStyle_PassesOutlierCountThrough()
    {
        var output = DistributionPlotRenderer.Render(new[] { Series() }, DurationUnit.Milliseconds, DistributionPlotStyle.BoxPlot);
        output.ShouldContain("(+2 outliers)");
    }

    [Fact]
    public void Render_EmptyInput_ReturnsEmpty()
    {
        DistributionPlotRenderer.Render(Array.Empty<DistributionPlotRenderer.Series>(), DurationUnit.Milliseconds, DistributionPlotStyle.Histogram).ShouldBeEmpty();
    }
}
