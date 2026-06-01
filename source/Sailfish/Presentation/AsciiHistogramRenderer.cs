using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sailfish.Presentation;

/// <summary>
/// Renders one or more <see cref="DistributionData"/> as compact horizontal histograms ("small
/// multiples") sharing a single, nice-rounded x-axis. Each distribution gets a block-glyph histogram
/// row (<c>▁▂▃▄▅▆▇█</c>) positioned on the shared axis so differences in location are visible, with a
/// thin marker line beneath it carrying the mean (<c>╵</c>) and median (<c>╿</c>) at their true x. A
/// summary table underneath lists exact n / outliers / min / max. No markers ever sit inside the
/// histogram blocks, avoiding the font side-bearing problems of the old inline box-plot style.
/// </summary>
public static class AsciiHistogramRenderer
{
    private const int DefaultPlotWidth = 60;
    private const int MinPlotWidth = 20;
    private const int Indent = 2;
    private const int LabelGap = 2;
    private const int MaxLabelWidth = 22;

    private const string Blocks = "▁▂▃▄▅▆▇█";
    private const char MeanMark = '╵';
    private const char MedianMark = '╿';
    private const char MeanMedianMark = '╽'; // mean and median land on the same column

    private const char AxisLine = '─';
    private const char AxisTick = '┬';
    private const char AxisLeftCap = '├';
    private const char AxisRightCap = '┤';

    /// <summary>
    /// Renders the supplied distributions. Returns an empty string when there is nothing to plot.
    /// </summary>
    /// <param name="distributions">Distributions to draw on a shared axis (1 = single, N = small multiples).</param>
    /// <param name="unit">Display unit; samples (ms) are converted to it for the axis and labels.</param>
    /// <param name="measure">Axis title prefix, e.g. "Time" → "Time (ms)".</param>
    /// <param name="plotWidth">Width of the plot area in characters.</param>
    public static string Render(IReadOnlyList<DistributionData> distributions, DurationUnit unit, string measure = "Time", int plotWidth = DefaultPlotWidth, int bins = 12)
    {
        if (distributions is null) return string.Empty;

        var drawable = distributions.Where(d => d is { IsEmpty: false }).ToList();
        if (drawable.Count == 0) return string.Empty;

        var allUnitValues = drawable
            .SelectMany(d => d.Samples)
            .Select(ms => DurationFormatter.ToUnit(ms, unit))
            .Where(double.IsFinite)
            .ToList();
        if (allUnitValues.Count == 0) return string.Empty;

        var axis = NiceAxis.Compute(allUnitValues.Min(), allUnitValues.Max());

        plotWidth = Math.Max(MinPlotWidth, plotWidth);
        var leftLabel = Format(axis.Min, axis.Decimals);
        var rightLabel = Format(axis.Max, axis.Decimals);

        // The label field must hold the longest distribution name AND leave room for the right-aligned
        // left-axis endpoint label (plus a separating space) so the histograms and axis '├' line up.
        var labelWidth = drawable.Max(d => Fit(d.Label).Length);
        labelWidth = Math.Max(labelWidth, leftLabel.Length + 1);
        labelWidth = Math.Clamp(labelWidth, 1, Math.Max(MaxLabelWidth, leftLabel.Length + 1));
        var plotLeft = Indent + labelWidth + LabelGap;

        var span = axis.Max - axis.Min;

        int Col(double unitValue)
        {
            if (span <= 0) return plotWidth / 2;
            var c = (int)Math.Round((unitValue - axis.Min) / span * (plotWidth - 1));
            return Math.Clamp(c, 0, plotWidth - 1);
        }

        var sb = new StringBuilder();
        var anyCombinedMarker = false;

        // Unit title, right-aligned to the plot's right edge.
        sb.AppendLine(PadLeftTo($"{measure} ({DurationFormatter.UnitLabel(unit)})", plotLeft + plotWidth));
        sb.AppendLine();

        // Histogram + marker row per distribution.
        foreach (var d in drawable)
        {
            var hist = BuildHistogramRow(d, unit, plotWidth, bins, axis.Min, span, Col);

            sb.Append(new string(' ', Indent)).Append(Fit(d.Label).PadRight(labelWidth)).Append(new string(' ', LabelGap));
            sb.Append(new string(hist).TrimEnd());
            sb.AppendLine();

            // Marker line directly beneath, scale-aligned.
            var markers = new char[plotWidth];
            Array.Fill(markers, ' ');
            var meanCol = Col(DurationFormatter.ToUnit(d.Mean, unit));
            var medianCol = Col(DurationFormatter.ToUnit(d.Median, unit));
            if (meanCol == medianCol)
            {
                markers[meanCol] = MeanMedianMark;
                anyCombinedMarker = true;
            }
            else
            {
                markers[meanCol] = MeanMark;
                markers[medianCol] = MedianMark;
            }

            var markerLine = (new string(' ', plotLeft) + new string(markers)).TrimEnd();
            if (markerLine.Length > 0) sb.AppendLine(markerLine);
        }

        sb.AppendLine();

        // Shared axis line with rounded endpoints and interior ticks.
        sb.AppendLine(BuildAxisLine(plotLeft, plotWidth, leftLabel, rightLabel, axis, Col));
        sb.AppendLine(BuildTickLabelLine(plotLeft, plotWidth, axis, Col));
        sb.AppendLine();

        // Legend.
        var legend = $"{new string(' ', Indent)}{MeanMark} mean   {MedianMark} median   {Blocks} count per bin";
        if (anyCombinedMarker) legend += $"   {MeanMedianMark} mean≈median";
        sb.AppendLine(legend);
        sb.AppendLine();

        // Summary table.
        foreach (var d in drawable)
        {
            var min = d.Samples.Min();
            var max = d.Samples.Max();
            sb.Append(new string(' ', Indent)).Append(Fit(d.Label).PadRight(labelWidth));
            sb.Append($"  n={d.Samples.Count}  outliers={d.OutlierCount}  min={Format(DurationFormatter.ToUnit(min, unit), 3)}  max={Format(DurationFormatter.ToUnit(max, unit), 3)}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildAxisLine(int plotLeft, int plotWidth, string leftLabel, string rightLabel, NiceAxisResult axis, Func<double, int> col)
    {
        var total = plotLeft + plotWidth + 1 + rightLabel.Length;
        var buf = new char[total];
        Array.Fill(buf, ' ');

        for (var c = 0; c < plotWidth; c++) buf[plotLeft + c] = AxisLine;
        buf[plotLeft] = AxisLeftCap;
        buf[plotLeft + plotWidth - 1] = AxisRightCap;

        // Interior ticks (skip the two endpoints, which are the caps).
        for (var i = 1; i < axis.Ticks.Count - 1; i++)
        {
            var c = col(axis.Ticks[i]);
            if (c > 0 && c < plotWidth - 1) buf[plotLeft + c] = AxisTick;
        }

        // Left endpoint label, right-aligned ending one space before the left cap.
        PlaceText(buf, plotLeft - 1 - leftLabel.Length, leftLabel);
        // Right endpoint label, one space after the right cap.
        PlaceText(buf, plotLeft + plotWidth + 1, rightLabel);

        return new string(buf).TrimEnd();
    }

    private static string BuildTickLabelLine(int plotLeft, int plotWidth, NiceAxisResult axis, Func<double, int> col)
    {
        var buf = new char[plotLeft + plotWidth + 8];
        Array.Fill(buf, ' ');

        // Only interior ticks get labels here; the endpoints are labelled on the axis line.
        for (var i = 1; i < axis.Ticks.Count - 1; i++)
        {
            var text = Format(axis.Ticks[i], axis.Decimals);
            var centre = plotLeft + col(axis.Ticks[i]);
            PlaceText(buf, centre - text.Length / 2, text);
        }

        return new string(buf).TrimEnd();
    }

    // Bins a distribution's in-range samples into <paramref name="bins"/> bins over its own range, then
    // paints each bin's glyph across the columns that bin spans on the SHARED axis. Coarse bins keep a
    // sparse tail contiguous (no isolated single-column "dust") while position still reflects the shared
    // scale, and the block width grows with the distribution's spread.
    private static char[] BuildHistogramRow(DistributionData d, DurationUnit unit, int plotWidth, int bins, double axisMin, double span, Func<double, int> col)
    {
        var hist = new char[plotWidth];
        Array.Fill(hist, ' ');

        var values = d.Samples.Select(ms => DurationFormatter.ToUnit(ms, unit)).Where(double.IsFinite).ToArray();
        if (values.Length == 0) return hist;

        var dMin = values.Min();
        var dMax = values.Max();
        if (dMax - dMin <= 0)
        {
            hist[col(dMin)] = Blocks[^1]; // identical values: a single full-height spike
            return hist;
        }

        var nBins = Math.Clamp(bins, 1, values.Length);
        var binWidth = (dMax - dMin) / nBins;
        var counts = new int[nBins];
        foreach (var v in values)
        {
            counts[Math.Clamp((int)((v - dMin) / binWidth), 0, nBins - 1)]++;
        }

        var maxCount = counts.Max();
        var startCol = col(dMin);
        var endCol = col(dMax);
        for (var c = startCol; c <= endCol; c++)
        {
            var valueAtCol = span <= 0 ? dMin : axisMin + span * (c / (double)(plotWidth - 1));
            var idx = Math.Clamp((int)((valueAtCol - dMin) / binWidth), 0, nBins - 1);
            hist[c] = Glyph(counts[idx], maxCount);
        }

        return hist;
    }

    private static char Glyph(int count, int maxCount)
    {
        if (count <= 0) return ' ';
        if (maxCount <= 0) return Blocks[0];
        var level = (int)Math.Ceiling((double)count / maxCount * Blocks.Length);
        level = Math.Clamp(level, 1, Blocks.Length);
        return Blocks[level - 1];
    }

    private static string Fit(string? label)
    {
        label ??= string.Empty;
        if (label.Length <= MaxLabelWidth) return label;
        return MaxLabelWidth <= 1 ? label[..MaxLabelWidth] : label[..(MaxLabelWidth - 1)] + "…";
    }

    private static string Format(double value, int decimals)
        => value.ToString("F" + Math.Max(0, decimals), System.Globalization.CultureInfo.InvariantCulture);

    private static string PadLeftTo(string text, int width)
        => text.Length >= width ? text : new string(' ', width - text.Length) + text;

    private static void PlaceText(char[] buffer, int start, string text)
    {
        if (start < 0) start = 0;
        for (var i = 0; i < text.Length && start + i < buffer.Length; i++)
        {
            buffer[start + i] = text[i];
        }
    }
}
