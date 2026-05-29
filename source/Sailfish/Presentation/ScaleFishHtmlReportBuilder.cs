using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.CurveFitting;

namespace Sailfish.Presentation;

/// <summary>
/// Renders a standalone HTML report for a ScaleFish run. Pure server-side: no external libraries or
/// browser-only JavaScript dependencies — everything is inline SVG + CSS so the file works offline.
/// </summary>
public static class ScaleFishHtmlReportBuilder
{
    private const int PlotWidth = 640;
    private const int PlotHeight = 320;
    private const int Padding = 50;

    /// <summary>
    /// Renders <paramref name="results"/> to an HTML string. Pass per-property X/Y measurements via
    /// <paramref name="measurementsByKey"/> so the SVG can plot empirical points alongside the fitted
    /// curve; pass an empty dictionary if measurements aren't available (curve-only rendering).
    /// </summary>
    public static string Build(
        IReadOnlyList<ScalefishClassModel> results,
        IReadOnlyDictionary<string, ComplexityMeasurement[]> measurementsByKey)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"en\"><head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<title>ScaleFish Report</title>");
        AppendStyles(sb);
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h1>ScaleFish complexity report</h1>");
        sb.AppendLine($"<p class=\"meta\">Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");

        foreach (var classModel in results)
        {
            sb.AppendLine($"<h2>{Escape(classModel.NameSpace)}.{Escape(classModel.TestClassName)}</h2>");
            foreach (var methodModel in classModel.ScaleFishMethodModels)
            {
                sb.AppendLine($"<h3>{Escape(methodModel.TestMethodName)}</h3>");
                foreach (var propModel in methodModel.ScaleFishPropertyModels)
                {
                    var key = propModel.PropertyName;
                    measurementsByKey.TryGetValue(key, out var measurements);
                    AppendPropertyBlock(sb, propModel, measurements);
                }
            }
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static void AppendStyles(StringBuilder sb)
    {
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 980px; margin: 2em auto; color: #1a1a1a; padding: 0 1em; }");
        sb.AppendLine("h1 { border-bottom: 2px solid #333; padding-bottom: .3em; }");
        sb.AppendLine("h2 { color: #2a4d8f; margin-top: 1.4em; }");
        sb.AppendLine("h3 { color: #444; }");
        sb.AppendLine(".meta { color: #777; font-size: .9em; }");
        sb.AppendLine(".prop-block { border: 1px solid #e0e0e0; border-radius: 6px; padding: 1em; margin: 1em 0; background: #fafafa; }");
        sb.AppendLine(".prop-key { font-family: ui-monospace, monospace; font-weight: 600; color: #2a4d8f; }");
        sb.AppendLine(".badge { display: inline-block; padding: .15em .55em; border-radius: 4px; font-size: .85em; margin-left: .5em; }");
        sb.AppendLine(".badge-good { background: #d6f4d6; color: #2e6e2e; }");
        sb.AppendLine(".badge-warn { background: #fef0c4; color: #7a5a00; }");
        sb.AppendLine("table { border-collapse: collapse; margin: .5em 0; font-size: .92em; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: .35em .8em; text-align: left; }");
        sb.AppendLine("th { background: #f0f0f0; }");
        sb.AppendLine("svg { background: white; border: 1px solid #ddd; border-radius: 4px; }");
        sb.AppendLine(".empirical { fill: #2a4d8f; }");
        sb.AppendLine(".curve-best { stroke: #2a4d8f; fill: none; stroke-width: 2; }");
        sb.AppendLine(".curve-next { stroke: #a06030; fill: none; stroke-width: 1.5; stroke-dasharray: 4 3; }");
        sb.AppendLine(".axis { stroke: #999; stroke-width: 1; }");
        sb.AppendLine(".axis-label { font-size: 11px; fill: #555; }");
        sb.AppendLine(".legend-text { font-size: 11px; fill: #333; }");
        sb.AppendLine("</style>");
    }

    private static void AppendPropertyBlock(StringBuilder sb, ScaleFishPropertyModel propModel, ComplexityMeasurement[]? measurements)
    {
        var model = propModel.ScaleFishModel;
        sb.AppendLine("<div class=\"prop-block\">");
        sb.AppendLine($"<div><span class=\"prop-key\">{Escape(propModel.PropertyName)}</span>");
        sb.AppendLine($"<span class=\"badge {(model.IsDistinguishable ? "badge-good" : "badge-warn")}\">{model.ScaleFishModelFunction.OName}{(model.IsDistinguishable ? "" : " (uncertain)")}</span></div>");

        AppendMetricsTable(sb, model);

        if (measurements is { Length: > 0 })
        {
            AppendPlot(sb, model, measurements);
        }

        if (model.TailFits is { Count: > 0 })
        {
            AppendTailFitsBlock(sb, model.TailFits);
        }

        sb.AppendLine("</div>");
    }

    private static void AppendMetricsTable(StringBuilder sb, ScaleFishModel model)
    {
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Metric</th><th>Value</th></tr>");
        sb.AppendLine($"<tr><td>Best fit</td><td>{Escape(model.ScaleFishModelFunction.Name)} — {Escape(model.ScaleFishModelFunction.OName)} (R²={F2(model.GoodnessOfFit)})</td></tr>");
        sb.AppendLine($"<tr><td>Runner-up</td><td>{Escape(model.NextClosestScaleFishModelFunction.Name)} — {Escape(model.NextClosestScaleFishModelFunction.OName)} (R²={F2(model.NextClosestGoodnessOfFit)})</td></tr>");
        sb.AppendLine($"<tr><td>Δ AICc</td><td>{F2(model.DeltaAicc)} (weight={F3(model.AkaikeWeight)})</td></tr>");
        sb.AppendLine($"<tr><td>Distinguishable</td><td>{(model.IsDistinguishable ? "yes" : "no")}</td></tr>");
        if (model.PowerLog is not null)
        {
            sb.AppendLine($"<tr><td>Continuous exponent</td><td>{Escape(model.PowerLog.Describe())} (R²={F2(model.PowerLog.RSquared)})</td></tr>");
        }
        if (model.CrossValidation is not null)
        {
            sb.AppendLine($"<tr><td>CV rank agreement</td><td>{F2(model.CrossValidation.RankAgreement)} ({model.CrossValidation.FoldCount} folds)</td></tr>");
        }
        if (model.Bootstrap is not null)
        {
            sb.AppendLine($"<tr><td>Bootstrap (n={model.Bootstrap.Iterations})</td><td>selection agreement {F2(model.Bootstrap.SelectionAgreement)} · scale 95 % CI [{F3(model.Bootstrap.ScaleCiLower)}, {F3(model.Bootstrap.ScaleCiUpper)}]</td></tr>");
        }
        if (model.SuggestedNextN.HasValue)
        {
            sb.AppendLine($"<tr><td>Suggested next N</td><td>{model.SuggestedNextN.Value}</td></tr>");
        }
        sb.AppendLine("</table>");
    }

    private static void AppendTailFitsBlock(StringBuilder sb, IReadOnlyList<TailFitResult> tailFits)
    {
        sb.AppendLine("<details><summary>Tail-percentile fits</summary>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Percentile</th><th>Best</th><th>R²</th><th>Δ AICc</th><th>Distinguishable</th></tr>");
        foreach (var t in tailFits.OrderBy(t => t.Percentile))
        {
            sb.AppendLine($"<tr><td>p{(int)Math.Round(t.Percentile * 100)}</td><td>{Escape(t.BestFamilyOName)}</td><td>{F2(t.BestRSquared)}</td><td>{F2(t.DeltaAicc)}</td><td>{(t.IsDistinguishable ? "yes" : "no")}</td></tr>");
        }
        sb.AppendLine("</table>");
        sb.AppendLine("</details>");
    }

    private static void AppendPlot(StringBuilder sb, ScaleFishModel model, ComplexityMeasurement[] measurements)
    {
        var xs = measurements.Select(m => m.X).Where(double.IsFinite).ToArray();
        var ys = measurements.Select(m => m.Y).Where(double.IsFinite).ToArray();
        if (xs.Length == 0 || ys.Length == 0) return;

        var xMin = xs.Min();
        var xMax = xs.Max();
        var yMin = Math.Min(0, ys.Min());
        var yMax = ys.Max();
        var bestFn = model.ScaleFishModelFunction;
        var nextFn = model.NextClosestScaleFishModelFunction;

        // Extend Y range to include both fitted curves so they don't clip.
        var allYs = new List<double>(ys);
        if (bestFn.FunctionParameters is not null) allYs.AddRange(SampleCurve(bestFn, xMin, xMax));
        if (nextFn?.FunctionParameters is not null) allYs.AddRange(SampleCurve(nextFn, xMin, xMax));
        var finiteYs = allYs.Where(double.IsFinite).ToArray();
        if (finiteYs.Length > 0)
        {
            yMin = Math.Min(yMin, finiteYs.Min());
            yMax = Math.Max(yMax, finiteYs.Max());
        }
        if (yMax <= yMin) yMax = yMin + 1;

        double XPix(double x) => Padding + (PlotWidth - 2 * Padding) * (x - xMin) / Math.Max(xMax - xMin, 1e-12);
        double YPix(double y) => PlotHeight - Padding - (PlotHeight - 2 * Padding) * (y - yMin) / Math.Max(yMax - yMin, 1e-12);

        sb.AppendLine($"<svg width=\"{PlotWidth}\" height=\"{PlotHeight}\" xmlns=\"http://www.w3.org/2000/svg\">");
        // Axes
        sb.AppendLine($"<line class=\"axis\" x1=\"{Padding}\" y1=\"{PlotHeight - Padding}\" x2=\"{PlotWidth - Padding}\" y2=\"{PlotHeight - Padding}\"/>");
        sb.AppendLine($"<line class=\"axis\" x1=\"{Padding}\" y1=\"{Padding}\" x2=\"{Padding}\" y2=\"{PlotHeight - Padding}\"/>");
        sb.AppendLine($"<text class=\"axis-label\" x=\"{PlotWidth / 2}\" y=\"{PlotHeight - 10}\" text-anchor=\"middle\">X (input scale)</text>");
        sb.AppendLine($"<text class=\"axis-label\" x=\"15\" y=\"{PlotHeight / 2}\" transform=\"rotate(-90, 15, {PlotHeight / 2})\" text-anchor=\"middle\">Y (mean, ms)</text>");

        // Fitted curves
        if (bestFn.FunctionParameters is not null) AppendCurvePath(sb, bestFn, xMin, xMax, XPix, YPix, "curve-best");
        if (nextFn?.FunctionParameters is not null) AppendCurvePath(sb, nextFn, xMin, xMax, XPix, YPix, "curve-next");

        // Empirical points
        foreach (var m in measurements.Where(m => double.IsFinite(m.X) && double.IsFinite(m.Y)))
        {
            sb.AppendLine($"<circle class=\"empirical\" cx=\"{F2(XPix(m.X))}\" cy=\"{F2(YPix(m.Y))}\" r=\"3.5\"/>");
        }

        // Legend
        sb.AppendLine($"<rect x=\"{PlotWidth - Padding - 140}\" y=\"{Padding - 10}\" width=\"135\" height=\"42\" fill=\"white\" stroke=\"#ccc\" rx=\"3\"/>");
        sb.AppendLine($"<line class=\"curve-best\" x1=\"{PlotWidth - Padding - 130}\" y1=\"{Padding + 0}\" x2=\"{PlotWidth - Padding - 105}\" y2=\"{Padding + 0}\"/>");
        sb.AppendLine($"<text class=\"legend-text\" x=\"{PlotWidth - Padding - 100}\" y=\"{Padding + 3}\">{Escape(bestFn.OName)}</text>");
        sb.AppendLine($"<line class=\"curve-next\" x1=\"{PlotWidth - Padding - 130}\" y1=\"{Padding + 18}\" x2=\"{PlotWidth - Padding - 105}\" y2=\"{Padding + 18}\"/>");
        sb.AppendLine($"<text class=\"legend-text\" x=\"{PlotWidth - Padding - 100}\" y=\"{Padding + 21}\">{Escape(nextFn?.OName ?? "next")}</text>");

        sb.AppendLine("</svg>");
    }

    private static IEnumerable<double> SampleCurve(ScaleFishModelFunction fn, double xMin, double xMax, int samples = 50)
    {
        if (fn.FunctionParameters is null) yield break;
        for (var i = 0; i <= samples; i++)
        {
            var x = xMin + (xMax - xMin) * i / (double)samples;
            yield return fn.Compute(fn.FunctionParameters.Bias, fn.FunctionParameters.Scale, x);
        }
    }

    private static void AppendCurvePath(StringBuilder sb, ScaleFishModelFunction fn, double xMin, double xMax,
        Func<double, double> xPix, Func<double, double> yPix, string cssClass)
    {
        if (fn.FunctionParameters is null) return;
        const int samples = 80;
        var pathSb = new StringBuilder();
        var moved = false;
        for (var i = 0; i <= samples; i++)
        {
            var x = xMin + (xMax - xMin) * i / (double)samples;
            var y = fn.Compute(fn.FunctionParameters.Bias, fn.FunctionParameters.Scale, x);
            if (!double.IsFinite(y)) continue;
            var px = xPix(x);
            var py = yPix(y);
            pathSb.Append(moved ? "L" : "M");
            pathSb.AppendFormat(CultureInfo.InvariantCulture, "{0:F2},{1:F2} ", px, py);
            moved = true;
        }
        sb.AppendLine($"<path class=\"{cssClass}\" d=\"{pathSb}\"/>");
    }

    private static string F2(double v)
    {
        if (double.IsNaN(v)) return "n/a";
        if (double.IsInfinity(v)) return "∞";
        return v.ToString("F2", CultureInfo.InvariantCulture);
    }

    private static string F3(double v)
    {
        if (double.IsNaN(v)) return "n/a";
        if (double.IsInfinity(v)) return "∞";
        return v.ToString("F3", CultureInfo.InvariantCulture);
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
    }
}
