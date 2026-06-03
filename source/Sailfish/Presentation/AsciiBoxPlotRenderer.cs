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
/// Glyphs: <c>├─┤</c> whisker (min–max); a three-row rectangle (<c>┌─┐</c> / <c>└─┘</c>) for the IQR
/// box, with the whisker centre line running straight through the box sides (<c>┼</c>); a heavy
/// vertical (<c>┿</c>) for the median; and <c>×</c> for the mean. Each series spans three rows so the
/// IQR reads as a real box rather than a flat bar.
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

    // Fraction of the data range to pad onto each side of the axis. 0.25 each side leaves the data
    // spanning the central 2/3 of the lane, so whiskers sit around 1/6 and 5/6 of the width.
    private const double AxisPaddingFraction = 0.25;

    private const char Space = ' ';
    private const char WhiskerLine = '─';
    private const char WhiskerCapLow = '├';
    private const char WhiskerCapHigh = '┤';

    // The IQR box is a real rectangle drawn across three rows (top edge, whisker row, bottom edge).
    // The corners and the middle-row sides are connecting box-drawing glyphs, so the whisker line
    // flows straight into the box with no gap.
    private const char BoxTopLeft = '┌';
    private const char BoxTopRight = '┐';
    private const char BoxBottomLeft = '└';
    private const char BoxBottomRight = '┘';
    private const char BoxSide = '┼';  // Q1/Q3 sides on the whisker row — the whisker line runs through

    // Median: a heavy vertical through the box that the whisker centre line passes through (┿), capped
    // with light tees where it meets the box edges. Mean: a single glyph on the centre line.
    private const char MedianTop = '┬';
    private const char MedianLine = '┿';
    private const char MedianBottom = '┴';
    private const char MeanMark = '×';

    private const char RulerLine = '─';
    private const char RulerTick = '┬';
    private const char RulerCornerLow = '└';
    private const char RulerCornerHigh = '┘';

    /// <summary>
    /// Renders the supplied series. Returns an empty string when there is nothing finite to draw.
    /// </summary>
    /// <param name="series">Series to draw on a shared axis (1 = single box, 2 = comparison, N = group).</param>
    /// <param name="unit">Display unit for the axis (typically <see cref="DurationFormatter.SelectUnit(System.Collections.Generic.IEnumerable{double})"/>).</param>
    /// <param name="width">Plot width in characters. Captions sit above each box, so no line exceeds this.</param>
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

        // Pad the axis by a fraction of the data range on each side so the whiskers float inside the
        // lane (data spans ~2/3 of the width) instead of always pinning to both edges — otherwise every
        // plot looks maxed out and the shape is hard to read.
        var (axisMinMs, axisMaxMs) = PadAxis(axisValuesMs.Min(), axisValuesMs.Max());
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

        var decimals = AxisDecimals(spanU);

        var sb = new StringBuilder();

        // Header: unit label, right-aligned over the plot.
        sb.AppendLine(PadCentreOrRight($"Time ({DurationFormatter.UnitLabel(unit)})", width));
        sb.AppendLine();

        // Each series: a caption line (method name + sample count) ABOVE its box, with the box and the
        // shared axis all left-aligned at column 0. There is no label column, so a rendered line is never
        // wider than the plot itself — it won't wrap on a narrower terminal window.
        foreach (var s in drawable)
        {
            var (top, mid, bottom) = BuildBox(s, width, Col);

            sb.AppendLine(Caption(s.Label ?? string.Empty, CountLabel(s), width));
            sb.AppendLine(top.TrimEnd());
            sb.AppendLine(mid.TrimEnd());
            sb.AppendLine(bottom.TrimEnd());
            sb.AppendLine();
        }

        // Shared axis ruler + tick labels.
        sb.AppendLine(Ruler(width, spanU));
        sb.AppendLine(TickLabels(width, axisMinMs, axisMaxMs, unit, decimals, spanU));
        sb.AppendLine();

        // Legend.
        sb.Append(MedianLine).Append(" median  ")
            .Append(MeanMark).Append(" mean  ")
            .Append(BoxTopLeft).Append(WhiskerLine).Append(BoxTopRight).Append(" IQR box  ")
            .Append(WhiskerCapLow).Append(WhiskerLine).Append(WhiskerCapHigh).Append(" min–max");
        sb.AppendLine();

        return sb.ToString();
    }

    // Builds the three rows of one series' box-and-whisker: the box top edge, the whisker row (with the
    // box sides, the median line and the mean glyph), and the box bottom edge. All three share the same
    // column mapping, so they stack into a real rectangle on the shared axis.
    private static (string Top, string Mid, string Bottom) BuildBox(BoxPlotSeries s, int width, Func<double, int> col)
    {
        var top = new char[width];
        var mid = new char[width];
        var bottom = new char[width];
        Array.Fill(top, Space);
        Array.Fill(mid, Space);
        Array.Fill(bottom, Space);

        if (s.HasNoSpread)
        {
            // A single value (or an all-equal sample) has no box; just mark the point on the whisker row.
            mid[col(s.Median)] = MeanMark;
            return (new string(top), new string(mid), new string(bottom));
        }

        var minC = col(s.Min);
        var maxC = col(s.Max);
        var q1C = col(s.Q1);
        var q3C = col(s.Q3);
        var medianC = col(s.Median);
        var meanC = col(s.Mean);

        // Whisker line spans the cleaned min–max on the middle row, and runs straight through the box
        // interior (the box is an outline, not a fill — but its centre line stays connected, no gap).
        for (var i = minC; i <= maxC; i++)
            if (mid[i] == Space) mid[i] = WhiskerLine;

        if (minC < q1C) mid[minC] = WhiskerCapLow;
        if (maxC > q3C) mid[maxC] = WhiskerCapHigh;

        if (q3C > q1C)
        {
            // Box outline: top and bottom edges spanning the quartiles, with corners; on the whisker row
            // the sides are crossings (┼) so the centre line passes through them with no gap.
            for (var i = q1C; i <= q3C; i++)
            {
                top[i] = WhiskerLine;
                bottom[i] = WhiskerLine;
            }

            top[q1C] = BoxTopLeft;
            top[q3C] = BoxTopRight;
            bottom[q1C] = BoxBottomLeft;
            bottom[q3C] = BoxBottomRight;
            mid[q1C] = BoxSide;
            mid[q3C] = BoxSide;

            // Mean: a single glyph on the centre line (anywhere between the whiskers). Never let it
            // clobber a box side.
            if (meanC >= 0 && meanC < width && meanC != q1C && meanC != q3C)
                mid[meanC] = MeanMark;

            // Median: a heavy vertical through the box, crossing the centre line (┿) and capped with
            // light tees on the box edges.
            if (medianC > q1C && medianC < q3C)
            {
                top[medianC] = MedianTop;
                mid[medianC] = MedianLine;
                bottom[medianC] = MedianBottom;
            }
            else if (medianC >= 0 && medianC < width)
            {
                mid[medianC] = MedianLine;
            }
        }
        else
        {
            // Degenerate IQR (quartiles land on a single column): no room for a box, so draw just the
            // median plus the mean glyph on the whisker line.
            if (meanC >= 0 && meanC < width && meanC != medianC) mid[meanC] = MeanMark;
            if (medianC >= 0 && medianC < width) mid[medianC] = MedianLine;
        }

        return (new string(top), new string(mid), new string(bottom));
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

    // Symmetrically pads the data range so the whiskers don't pin to the lane edges. A zero-width
    // range (single value / all equal) is left untouched — the renderer centres it instead.
    private static (double Min, double Max) PadAxis(double dataMin, double dataMax)
    {
        var range = dataMax - dataMin;
        if (range <= 0) return (dataMin, dataMax);
        var pad = range * AxisPaddingFraction;
        return (dataMin - pad, dataMax + pad);
    }

    private static int AxisDecimals(double spanU)
    {
        if (spanU >= 100) return 0;
        if (spanU >= 10) return 1;
        if (spanU >= 1) return 2;
        return 3;
    }

    private static string CountLabel(BoxPlotSeries s)
        => s.OutlierCount > 0 ? $"n={s.N} (+{s.OutlierCount} outliers)" : $"n={s.N}";

    // "<label>  <count>" caption sitting above a box, trimmed so it never exceeds the plot width: the
    // count is kept whole and an over-long method name is ellipsized (or dropped if there's no room).
    private static string Caption(string label, string count, int width)
    {
        var maxLabel = width - 2 - count.Length;
        if (label.Length > maxLabel)
            label = maxLabel <= 1 ? string.Empty : label[..(maxLabel - 1)] + "…";
        return label.Length == 0 ? count : label + "  " + count;
    }

    private static string PadCentreOrRight(string text, int width)
    {
        if (text.Length >= width) return text;
        var leftPad = width - text.Length;
        return new string(Space, leftPad) + text;
    }
}
