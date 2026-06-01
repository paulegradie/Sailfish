using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sailfish.Presentation;

/// <summary>
/// Renders one or more <see cref="BoxPlotSeries"/> as a horizontal Unicode box-and-whisker plot on a
/// single shared axis. Output is plain monospace text, so it lines up in IDE test-output windows and
/// inside fenced code blocks in Markdown. No external dependencies.
/// <para>
/// Glyphs: <c>┣━┫</c> whisker (min–max), <c>▒</c> IQR box, <c>┃</c> median, <c>◆</c> mean.
/// The axis is scaled to the <em>cleaned</em> min–max so the distribution shape stays legible; any
/// outliers Sailfish removed are reported as a count beside the lane rather than stretching the axis
/// (their exact values are listed in the surrounding text). The axis unit (ns/µs/ms/s) is chosen by
/// <see cref="DurationFormatter"/> so fast benchmarks aren't flattened to a single point.
/// </para>
/// </summary>
public static class AsciiBoxPlotRenderer
{
    private const int DefaultWidth = 54;
    private const int MinWidth = 24;
    private const int MaxLabelWidth = 28;

    private const char Space = ' ';
    private const char WhiskerLine = '━';
    private const char WhiskerCapLow = '┣';
    private const char WhiskerCapHigh = '┫';
    private const char BoxFill = '▒';
    private const char BoxEdgeLow = '┫';
    private const char BoxEdgeHigh = '┣';
    private const char MedianMark = '┃';
    private const char MeanMark = '◆';
    private const char RulerLine = '─';
    private const char RulerTick = '┬';
    private const char RulerCornerLow = '└';
    private const char RulerCornerHigh = '┘';

    /// <summary>
    /// Renders the supplied series. Returns an empty string when there is nothing finite to draw.
    /// </summary>
    /// <param name="series">Series to draw on a shared axis (1 = single box, 2 = comparison, N = group).</param>
    /// <param name="unit">Display unit for the axis (typically <see cref="DurationFormatter.SelectUnit(System.Collections.Generic.IEnumerable{double})"/>).</param>
    /// <param name="width">Plot width in characters (the drawable lane, excluding the label column).</param>
    public static string Render(IReadOnlyList<BoxPlotSeries> series, DurationUnit unit, int width = DefaultWidth)
    {
        if (series is null) return string.Empty;

        var drawable = series.Where(s => s is { IsEmpty: false }).ToList();
        if (drawable.Count == 0) return string.Empty;

        // Axis spans the cleaned min–max only (outliers are reported as a count, not plotted, so a few
        // far-flung points can't compress the box out of view).
        var axisValuesMs = drawable
            .SelectMany(s => new[] { s.Min, s.Max })
            .Where(double.IsFinite)
            .ToList();
        if (axisValuesMs.Count == 0) return string.Empty;

        width = Math.Max(MinWidth, width);
        var axisMinMs = axisValuesMs.Min();
        var axisMaxMs = axisValuesMs.Max();
        var minU = DurationFormatter.ToUnit(axisMinMs, unit);
        var maxU = DurationFormatter.ToUnit(axisMaxMs, unit);
        var spanU = maxU - minU;

        // Column for a millisecond value. With zero span (all values equal) everything collapses to
        // the lane centre so the single marker is visible rather than jammed against the edge.
        int Col(double valueMs)
        {
            if (spanU <= 0) return width / 2;
            var u = DurationFormatter.ToUnit(valueMs, unit);
            var col = (int)Math.Round((u - minU) / spanU * (width - 1));
            return Math.Clamp(col, 0, width - 1);
        }

        var labelWidth = Math.Clamp(drawable.Max(s => (s.Label ?? string.Empty).Length), 0, MaxLabelWidth);
        var labelPad = new string(Space, labelWidth);
        var decimals = AxisDecimals(spanU);

        var sb = new StringBuilder();

        // Header: unit, right-aligned over the lane.
        var unitHeader = $"Time ({DurationFormatter.UnitLabel(unit)})";
        sb.Append(labelPad).Append("  ").AppendLine(PadCentreOrRight(unitHeader, width));

        // One lane per series.
        foreach (var s in drawable)
        {
            sb.Append(TruncatePad(s.Label ?? string.Empty, labelWidth)).Append("  ");
            sb.Append(Lane(s, width, Col));
            sb.Append("  n=").Append(s.N);
            if (s.OutlierCount > 0) sb.Append(" (+").Append(s.OutlierCount).Append(" outliers)");
            sb.AppendLine();
        }

        // Shared axis ruler + tick labels.
        sb.Append(labelPad).Append("  ").AppendLine(Ruler(width, spanU));
        sb.Append(labelPad).Append("  ").AppendLine(TickLabels(width, axisMinMs, axisMaxMs, unit, decimals, spanU));

        // Legend.
        sb.Append("  ").Append(MedianMark).Append(" median  ")
            .Append(MeanMark).Append(" mean  ")
            .Append(BoxFill).Append(" IQR box  ")
            .Append(WhiskerCapLow).Append(WhiskerLine).Append(WhiskerCapHigh).Append(" min–max");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string Lane(BoxPlotSeries s, int width, Func<double, int> col)
    {
        var buf = new char[width];
        for (var i = 0; i < width; i++) buf[i] = Space;

        if (s.HasNoSpread)
        {
            buf[col(s.Median)] = MeanMark;
            return new string(buf);
        }

        var minC = col(s.Min);
        var maxC = col(s.Max);
        var q1C = col(s.Q1);
        var q3C = col(s.Q3);

        // Layers, painted low-to-high precedence (later overwrites earlier).
        for (var i = minC; i <= maxC; i++)
        {
            if (buf[i] == Space) buf[i] = WhiskerLine;
        }

        for (var i = q1C; i <= q3C; i++) buf[i] = BoxFill;

        buf[q1C] = BoxEdgeLow;
        buf[q3C] = BoxEdgeHigh;

        if (minC < q1C) buf[minC] = WhiskerCapLow;
        if (maxC > q3C) buf[maxC] = WhiskerCapHigh;

        var meanC = col(s.Mean);
        if (meanC >= 0 && meanC < width) buf[meanC] = MeanMark;

        buf[col(s.Median)] = MedianMark; // highest precedence

        return new string(buf);
    }

    private static string Ruler(int width, double spanU)
    {
        var buf = new char[width];
        for (var i = 0; i < width; i++) buf[i] = RulerLine;

        if (spanU <= 0)
        {
            buf[width / 2] = RulerTick;
            return new string(buf);
        }

        foreach (var tickCol in TickColumns(width)) buf[tickCol] = RulerTick;
        buf[0] = RulerCornerLow;
        buf[width - 1] = RulerCornerHigh;
        return new string(buf);
    }

    private static string TickLabels(int width, double axisMinMs, double axisMaxMs, DurationUnit unit, int decimals, double spanU)
    {
        var buf = new char[width];
        for (var i = 0; i < width; i++) buf[i] = Space;

        IEnumerable<int> cols = spanU <= 0 ? new[] { width / 2 } : TickColumns(width);

        var lastEnd = -2;
        foreach (var tickCol in cols)
        {
            var fraction = width <= 1 ? 0 : tickCol / (double)(width - 1);
            var valueMs = axisMinMs + fraction * (axisMaxMs - axisMinMs);
            var text = DurationFormatter.Format(valueMs, unit, decimals);

            var start = tickCol - text.Length / 2;
            if (tickCol == 0) start = 0;
            if (tickCol == width - 1) start = width - text.Length;
            start = Math.Clamp(start, 0, Math.Max(0, width - text.Length));

            if (start <= lastEnd + 1) continue; // keep at least one space between labels
            for (var i = 0; i < text.Length && start + i < width; i++) buf[start + i] = text[i];
            lastEnd = start + text.Length - 1;
        }

        return new string(buf).TrimEnd();
    }

    // Five evenly spaced ticks across the lane (0%, 25%, 50%, 75%, 100%).
    private static IEnumerable<int> TickColumns(int width)
    {
        var seen = new HashSet<int>();
        foreach (var fraction in new[] { 0.0, 0.25, 0.5, 0.75, 1.0 })
        {
            var col = (int)Math.Round(fraction * (width - 1));
            if (seen.Add(col)) yield return col;
        }
    }

    private static int AxisDecimals(double spanU)
    {
        if (spanU >= 100) return 0;
        if (spanU >= 10) return 1;
        if (spanU >= 1) return 2;
        return 3;
    }

    private static string TruncatePad(string label, int labelWidth)
    {
        if (labelWidth == 0) return string.Empty;
        if (label.Length > labelWidth)
        {
            return labelWidth <= 1 ? label[..labelWidth] : label[..(labelWidth - 1)] + "…";
        }

        return label.PadRight(labelWidth);
    }

    private static string PadCentreOrRight(string text, int width)
    {
        if (text.Length >= width) return text;
        var leftPad = width - text.Length;
        return new string(Space, leftPad) + text;
    }
}
