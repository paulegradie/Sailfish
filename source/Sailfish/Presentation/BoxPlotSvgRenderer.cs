using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Sailfish.Presentation;

/// <summary>
/// Renders <see cref="BoxPlotSeries"/> as a self-contained inline <c>&lt;svg&gt;</c> box-and-whisker
/// chart. Pure server-side — hand-rolled SVG rects/lines, no JavaScript or external libraries — exactly
/// like <see cref="ScaleFishHtmlReportBuilder"/>, so the markup drops straight into an offline HTML file.
/// As with the ASCII renderer, the axis is scaled to the cleaned min–max and removed-outlier counts are
/// noted in the row label rather than stretching the axis.
/// </summary>
public static class BoxPlotSvgRenderer
{
    private const int LabelWidth = 160;
    private const int PadRight = 24;
    private const int PadTop = 30;
    private const int RowHeight = 38;
    private const int AxisHeight = 52;

    /// <summary>
    /// Renders the supplied series to an <c>&lt;svg&gt;</c> string. Returns an empty string when there
    /// is nothing finite to draw.
    /// </summary>
    public static string RenderSvg(IReadOnlyList<BoxPlotSeries> series, DurationUnit unit, int width = 720)
    {
        if (series is null) return string.Empty;

        var drawable = series.Where(s => s is { IsEmpty: false }).ToList();
        if (drawable.Count == 0) return string.Empty;

        var axisValuesMs = drawable.SelectMany(s => new[] { s.Min, s.Max }).Where(double.IsFinite).ToList();
        if (axisValuesMs.Count == 0) return string.Empty;

        width = Math.Max(360, width);
        var axisMinMs = axisValuesMs.Min();
        var axisMaxMs = axisValuesMs.Max();
        var minU = DurationFormatter.ToUnit(axisMinMs, unit);
        var maxU = DurationFormatter.ToUnit(axisMaxMs, unit);
        var spanU = maxU - minU;

        var plotLeft = LabelWidth;
        var plotRight = width - PadRight;
        var plotW = plotRight - plotLeft;
        var height = PadTop + drawable.Count * RowHeight + AxisHeight;

        double XPix(double valueMs)
        {
            if (spanU <= 0) return plotLeft + plotW / 2.0;
            var u = DurationFormatter.ToUnit(valueMs, unit);
            var x = plotLeft + plotW * (u - minU) / spanU;
            return Math.Clamp(x, plotLeft, plotRight);
        }

        var sb = new StringBuilder();
        sb.AppendLine($"<svg class=\"boxplot\" width=\"{width}\" height=\"{height}\" viewBox=\"0 0 {width} {height}\" xmlns=\"http://www.w3.org/2000/svg\">");

        // Unit label, top-right.
        sb.AppendLine($"<text class=\"bp-unit\" x=\"{plotRight}\" y=\"{PadTop - 12}\" text-anchor=\"end\">Time ({Escape(DurationFormatter.UnitLabel(unit))})</text>");

        for (var i = 0; i < drawable.Count; i++)
        {
            AppendRow(sb, drawable[i], PadTop + i * RowHeight + RowHeight / 2, XPix);
        }

        AppendAxis(sb, plotLeft, plotRight, PadTop + drawable.Count * RowHeight + 10, axisMinMs, axisMaxMs, unit, spanU);

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, BoxPlotSeries s, double cy, Func<double, double> xPix)
    {
        var label = s.Label ?? string.Empty;
        if (s.OutlierCount > 0) label += $" (+{s.OutlierCount} out)";
        sb.AppendLine($"<text class=\"bp-label\" x=\"8\" y=\"{F2(cy + 4)}\">{Escape(label)} · n={s.N}</text>");

        if (s.HasNoSpread)
        {
            sb.AppendLine($"<circle class=\"bp-mean\" cx=\"{F2(xPix(s.Median))}\" cy=\"{F2(cy)}\" r=\"3.5\"/>");
            return;
        }

        var boxHeight = RowHeight * 0.46;
        var boxTop = cy - boxHeight / 2;
        var minX = xPix(s.Min);
        var maxX = xPix(s.Max);
        var q1X = xPix(s.Q1);
        var q3X = xPix(s.Q3);
        var medX = xPix(s.Median);
        var meanX = xPix(s.Mean);
        var capTop = cy - boxHeight * 0.3;
        var capBottom = cy + boxHeight * 0.3;

        // Whisker line + end caps.
        sb.AppendLine($"<line class=\"bp-whisker\" x1=\"{F2(minX)}\" y1=\"{F2(cy)}\" x2=\"{F2(maxX)}\" y2=\"{F2(cy)}\"/>");
        sb.AppendLine($"<line class=\"bp-whisker\" x1=\"{F2(minX)}\" y1=\"{F2(capTop)}\" x2=\"{F2(minX)}\" y2=\"{F2(capBottom)}\"/>");
        sb.AppendLine($"<line class=\"bp-whisker\" x1=\"{F2(maxX)}\" y1=\"{F2(capTop)}\" x2=\"{F2(maxX)}\" y2=\"{F2(capBottom)}\"/>");

        // IQR box.
        sb.AppendLine($"<rect class=\"bp-box\" x=\"{F2(q1X)}\" y=\"{F2(boxTop)}\" width=\"{F2(Math.Max(1, q3X - q1X))}\" height=\"{F2(boxHeight)}\"/>");

        // Median line.
        sb.AppendLine($"<line class=\"bp-median\" x1=\"{F2(medX)}\" y1=\"{F2(boxTop)}\" x2=\"{F2(medX)}\" y2=\"{F2(boxTop + boxHeight)}\"/>");

        // Mean marker.
        sb.AppendLine($"<circle class=\"bp-mean\" cx=\"{F2(meanX)}\" cy=\"{F2(cy)}\" r=\"3.5\"/>");
    }

    private static void AppendAxis(StringBuilder sb, double left, double right, double y, double axisMinMs, double axisMaxMs, DurationUnit unit, double spanU)
    {
        sb.AppendLine($"<line class=\"bp-axis\" x1=\"{F2(left)}\" y1=\"{F2(y)}\" x2=\"{F2(right)}\" y2=\"{F2(y)}\"/>");

        var decimals = spanU >= 100 ? 0 : spanU >= 10 ? 1 : spanU >= 1 ? 2 : 3;
        const int ticks = 5;
        for (var i = 0; i < ticks; i++)
        {
            var fraction = i / (double)(ticks - 1);
            var x = left + (right - left) * fraction;
            var valueMs = axisMinMs + fraction * (axisMaxMs - axisMinMs);
            sb.AppendLine($"<line class=\"bp-axis\" x1=\"{F2(x)}\" y1=\"{F2(y)}\" x2=\"{F2(x)}\" y2=\"{F2(y + 5)}\"/>");
            sb.AppendLine($"<text class=\"bp-tick\" x=\"{F2(x)}\" y=\"{F2(y + 18)}\" text-anchor=\"middle\">{Escape(DurationFormatter.Format(valueMs, unit, decimals))}</text>");
        }

        // Legend.
        sb.AppendLine($"<text class=\"bp-legend\" x=\"8\" y=\"{F2(y + 18)}\">box = IQR · line = median · ● = mean</text>");
    }

    private static string F2(double v)
    {
        if (double.IsNaN(v)) return "0";
        if (double.IsInfinity(v)) return "0";
        return v.ToString("F2", CultureInfo.InvariantCulture);
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
