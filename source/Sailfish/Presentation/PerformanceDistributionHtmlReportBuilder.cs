using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;

namespace Sailfish.Presentation;

/// <summary>
/// Renders a standalone HTML report of box-and-whisker distribution plots for a run — one chart per
/// comparison group (or per class when there are no groups). Pure server-side inline SVG + CSS with no
/// external dependencies, mirroring <see cref="ScaleFishHtmlReportBuilder"/>, so the file opens offline
/// and embeds cleanly in a PR.
/// </summary>
public static class PerformanceDistributionHtmlReportBuilder
{
    private const int SvgWidth = 760;

    /// <summary>
    /// Builds the HTML document. Returns an empty string when there is nothing plottable, so callers can
    /// skip writing a file.
    /// </summary>
    public static string Build(IReadOnlyList<IClassExecutionSummary> summaries)
    {
        if (summaries is null || summaries.Count == 0) return string.Empty;

        var body = new System.Text.StringBuilder();
        var anyChart = false;

        foreach (var summary in summaries)
        {
            var className = summary.TestClass?.Name ?? "(unknown)";
            var classResults = summary.CompiledTestCaseResults
                .Where(r => r.PerformanceRunResult is { } pr && pr.DataWithOutliersRemoved.Length > 0)
                .ToList();
            if (classResults.Count == 0) continue;

            var classBody = new System.Text.StringBuilder();
            foreach (var group in classResults.GroupBy(r => r.GroupingId))
            {
                var series = group
                    .Select(r =>
                    {
                        var pr = r.PerformanceRunResult!;
                        return BoxPlotData.FromSamples(
                            r.TestCaseId!.DisplayName,
                            pr.DataWithOutliersRemoved,
                            pr.Mean,
                            pr.UpperOutliers.Concat(pr.LowerOutliers).ToArray());
                    })
                    .Where(s => !s.IsEmpty)
                    .ToList();
                if (series.Count == 0) continue;

                var unit = DurationFormatter.SelectUnit(series.SelectMany(s => new[] { s.Min, s.Max }));
                var svg = BoxPlotSvgRenderer.RenderSvg(series, unit, SvgWidth);
                if (string.IsNullOrEmpty(svg)) continue;

                if (!string.IsNullOrEmpty(group.Key))
                {
                    classBody.AppendLine($"<h3>{Escape(group.Key!)}</h3>");
                }

                classBody.AppendLine("<div class=\"chart\">");
                classBody.AppendLine(svg);
                classBody.AppendLine("</div>");
                anyChart = true;
            }

            if (classBody.Length > 0)
            {
                body.AppendLine($"<h2>{Escape(className)}</h2>");
                body.Append(classBody);
            }
        }

        if (!anyChart) return string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\"><head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<title>Sailfish Distribution Report</title>");
        AppendStyles(sb);
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h1>Sailfish distribution report</h1>");
        sb.AppendLine($"<p class=\"meta\">Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.Append(body);
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static void AppendStyles(System.Text.StringBuilder sb)
    {
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 980px; margin: 2em auto; color: #1a1a1a; padding: 0 1em; }");
        sb.AppendLine("h1 { border-bottom: 2px solid #333; padding-bottom: .3em; }");
        sb.AppendLine("h2 { color: #2a4d8f; margin-top: 1.4em; }");
        sb.AppendLine("h3 { color: #444; }");
        sb.AppendLine(".meta { color: #777; font-size: .9em; }");
        sb.AppendLine(".chart { border: 1px solid #e0e0e0; border-radius: 6px; padding: .5em; margin: 1em 0; background: #fafafa; }");
        sb.AppendLine("svg.boxplot { background: white; }");
        sb.AppendLine(".bp-label { font-size: 12px; fill: #333; font-family: ui-monospace, monospace; }");
        sb.AppendLine(".bp-unit { font-size: 11px; fill: #777; }");
        sb.AppendLine(".bp-tick { font-size: 10px; fill: #555; }");
        sb.AppendLine(".bp-legend { font-size: 10px; fill: #777; }");
        sb.AppendLine(".bp-whisker { stroke: #555; stroke-width: 1.5; }");
        sb.AppendLine(".bp-box { fill: #cfe0f7; stroke: #2a4d8f; stroke-width: 1.5; }");
        sb.AppendLine(".bp-median { stroke: #1a1a1a; stroke-width: 2; }");
        sb.AppendLine(".bp-mean { fill: #c0392b; }");
        sb.AppendLine(".bp-axis { stroke: #999; stroke-width: 1; }");
        sb.AppendLine("</style>");
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
