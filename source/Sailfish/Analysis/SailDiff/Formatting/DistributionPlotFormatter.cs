using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public.Models;
using Sailfish.Presentation;

namespace Sailfish.Analysis.SailDiff.Formatting;

/// <summary>
/// Builds the histogram distribution plot that accompanies a SailDiff comparison. For a single
/// comparison it draws two histogram rows (primary vs compared) on a shared axis; for many comparisons
/// it draws each distinct method once. The sample data comes from <see cref="StatisticalTestResult.RawDataBefore"/>
/// / <see cref="StatisticalTestResult.RawDataAfter"/> (already cleaned by the test), so no extra plumbing
/// is needed. Rendering is delegated to <see cref="AsciiHistogramRenderer"/>.
/// </summary>
public interface IDistributionPlotFormatter
{
    /// <summary>Plot for a single comparison (two histogram rows). Empty when plots are disabled or unavailable.</summary>
    string CreatePlot(SailDiffComparisonData data, OutputContext context);

    /// <summary>Plot for several comparisons (one histogram row per distinct method, shared axis).</summary>
    string CreatePlot(IEnumerable<SailDiffComparisonData> comparisons, OutputContext context);
}

/// <inheritdoc cref="IDistributionPlotFormatter"/>
public class DistributionPlotFormatter : IDistributionPlotFormatter
{
    private const int PlotWidth = 60;
    private readonly IRunSettings? _runSettings;

    /// <summary>Parameterless constructor — plots default on (used by the factory and tests).</summary>
    public DistributionPlotFormatter()
    {
    }

    /// <summary>
    /// DI constructor. When <paramref name="runSettings"/> is supplied, the global
    /// <see cref="IRunSettings.EnableDistributionPlots"/> flag gates every comparison plot in one place.
    /// </summary>
    public DistributionPlotFormatter(IRunSettings? runSettings)
    {
        _runSettings = runSettings;
    }

    private bool PlotsEnabled => _runSettings?.EnableDistributionPlots ?? true;

    public string CreatePlot(SailDiffComparisonData data, OutputContext context)
    {
        if (data is null || context == OutputContext.Csv) return string.Empty;
        if (!PlotsEnabled || !data.Metadata.IncludeDistributionPlot) return string.Empty;
        if (data.Statistics is null || data.Statistics.Failed) return string.Empty;

        var (primaryData, comparedData) = OrientedSamples(data);
        var series = new List<DistributionPlotRenderer.Series>
        {
            new(data.PrimaryMethodName, primaryData ?? Array.Empty<double>(), double.NaN, double.NaN, null),
            new(data.ComparedMethodName, comparedData ?? Array.Empty<double>(), double.NaN, double.NaN, null)
        };

        return Wrap(RenderSeries(series), context);
    }

    public string CreatePlot(IEnumerable<SailDiffComparisonData> comparisons, OutputContext context)
    {
        var list = comparisons?.ToList() ?? new List<SailDiffComparisonData>();
        if (list.Count == 0 || context == OutputContext.Csv) return string.Empty;
        if (!PlotsEnabled || !list[0].Metadata.IncludeDistributionPlot) return string.Empty;

        // One row per distinct method (a method shared across comparisons is drawn once).
        var seen = new HashSet<string>();
        var series = new List<DistributionPlotRenderer.Series>();
        foreach (var data in list)
        {
            if (data.Statistics is null || data.Statistics.Failed) continue;
            var (primaryData, comparedData) = OrientedSamples(data);
            AddDistinct(series, seen, data.PrimaryMethodName, primaryData);
            AddDistinct(series, seen, data.ComparedMethodName, comparedData);
        }

        return Wrap(RenderSeries(series), context);
    }

    // Honors the perspective swap the other SailDiff formatters apply: when the comparison is viewed
    // from the compared method's perspective, before/after are exchanged.
    private static (double[]? Primary, double[]? Compared) OrientedSamples(SailDiffComparisonData data)
    {
        var swap = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName;
        return swap
            ? (data.Statistics.RawDataAfter, data.Statistics.RawDataBefore)
            : (data.Statistics.RawDataBefore, data.Statistics.RawDataAfter);
    }

    private static void AddDistinct(List<DistributionPlotRenderer.Series> series, HashSet<string> seen, string label, double[]? data)
    {
        if (data is null || data.Length == 0) return;
        if (!seen.Add(label)) return;
        series.Add(new DistributionPlotRenderer.Series(label, data, double.NaN, double.NaN, null));
    }

    private string RenderSeries(IReadOnlyList<DistributionPlotRenderer.Series> series)
    {
        var drawable = series.Where(s => s.Samples is { Count: > 0 }).ToList();
        if (drawable.Count == 0) return string.Empty;

        var unit = DurationFormatter.SelectUnit(drawable.SelectMany(s => s.Samples));
        var style = _runSettings?.DistributionPlotStyle ?? DistributionPlotStyle.Histogram;
        return DistributionPlotRenderer.Render(drawable, unit, style, PlotWidth);
    }

    private static string Wrap(string plot, OutputContext context)
    {
        if (string.IsNullOrEmpty(plot)) return string.Empty;

        return context switch
        {
            // Fenced so monospace alignment survives GitHub / IDE Markdown rendering.
            OutputContext.Markdown => "**Distribution**\n\n```text\n" + plot + "```\n",
            OutputContext.Ide => "📊 DISTRIBUTION\n" + plot,
            OutputContext.Console => "DISTRIBUTION\n" + plot,
            _ => string.Empty
        };
    }
}
