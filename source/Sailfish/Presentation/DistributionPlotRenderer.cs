using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Presentation;

/// <summary>
/// Which terminal/Markdown distribution visualization to render.
/// </summary>
public enum DistributionPlotStyle
{
    /// <summary>Compact shared-axis histograms (default).</summary>
    Histogram = 0,

    /// <summary>Horizontal box-and-whisker plots.</summary>
    BoxPlot = 1
}

/// <summary>
/// Single entry point for rendering distribution plots, dispatching to the configured
/// <see cref="DistributionPlotStyle"/>. Callers describe each series once (label + raw in-range samples
/// + mean/median + removed outliers) and this builds the renderer-specific model, so the style is
/// chosen in one place rather than branched across every output surface.
/// </summary>
public static class DistributionPlotRenderer
{
    private const int DefaultWidth = 60;

    /// <summary>One distribution to render: raw in-range samples (ms) plus its statistics.</summary>
    public readonly record struct Series(
        string Label,
        IReadOnlyList<double> Samples,
        double Mean,
        double Median,
        IReadOnlyList<double>? Outliers);

    /// <summary>
    /// Renders the supplied series in the requested style. Returns an empty string when there is
    /// nothing to draw.
    /// </summary>
    public static string Render(IReadOnlyList<Series> series, DurationUnit unit, DistributionPlotStyle style, int width = DefaultWidth)
    {
        if (series is null || series.Count == 0) return string.Empty;

        return style == DistributionPlotStyle.BoxPlot
            ? AsciiBoxPlotRenderer.Render(
                series.Select(s => BoxPlotData.FromSamples(s.Label, s.Samples, s.Mean, s.Outliers)).ToList(),
                unit,
                width)
            : AsciiHistogramRenderer.Render(
                series.Select(s => DistributionData.FromSamples(s.Label, s.Samples, s.Mean, s.Median, s.Outliers?.Count ?? 0)).ToList(),
                unit,
                plotWidth: width);
    }
}
