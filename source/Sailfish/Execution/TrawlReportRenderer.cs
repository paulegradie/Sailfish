using System;
using System.Text;
using Sailfish.Contracts.Public.Models;
using Sailfish.Presentation;

namespace Sailfish.Execution;

/// <summary>
///     Renders a Trawl run into a Markdown/console report: a summary line, a latency-percentile table, the
///     latency distribution (reusing <see cref="DistributionPlotRenderer" /> so it honors the configured
///     plot style), and Unicode sparklines for throughput and p99 over time.
/// </summary>
internal static class TrawlReportRenderer
{
    private const string SparkChars = "▁▂▃▄▅▆▇█";

    public static string Render(TrawlResult result, DistributionPlotStyle plotStyle)
    {
        var latency = result.Latency;
        var sb = new StringBuilder();

        sb.AppendLine($"## Trawl — {result.DisplayName}");
        sb.AppendLine();
        sb.AppendLine($"- Model: **{result.Model}** | Virtual users: **{result.VirtualUsers}** | Duration: **{result.Duration.TotalSeconds:0.##}s**");
        sb.AppendLine($"- Throughput: **{result.RequestsPerSecond:0.#} req/s** | Requests: **{result.TotalRequests}** | Errors: **{result.TotalErrors}** ({result.ErrorRate:0.##%})");
        sb.AppendLine();
        sb.AppendLine("| latency (ms) | p50 | p90 | p95 | p99 | max | mean | min |");
        sb.AppendLine("|---|----:|----:|----:|----:|----:|-----:|----:|");
        sb.AppendLine($"| | {latency.P50:0.##} | {latency.P90:0.##} | {latency.P95:0.##} | {latency.P99:0.##} | {latency.Max:0.##} | {latency.Mean:0.##} | {latency.Min:0.##} |");
        sb.AppendLine();

        if (result.LatencySamplesMs.Length > 0)
        {
            var plot = DistributionPlotRenderer.Render(
                new[] { new DistributionPlotRenderer.Series("Latency (ms)", result.LatencySamplesMs, latency.Mean, latency.P50, null) },
                DurationUnit.Milliseconds,
                plotStyle);

            if (!string.IsNullOrWhiteSpace(plot))
            {
                sb.AppendLine("```");
                sb.AppendLine(plot.TrimEnd());
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        if (result.TimeSeries is { } timeSeries && timeSeries.RequestsPerSecond.Length > 0)
        {
            sb.AppendLine($"Throughput/s : {Sparkline(timeSeries.RequestsPerSecond)}");
            sb.AppendLine($"p99 (ms)/s   : {Sparkline(timeSeries.P99Ms)}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>Maps a series of values onto Unicode block characters by normalized magnitude.</summary>
    internal static string Sparkline(double[] values)
    {
        if (values is null || values.Length == 0) return string.Empty;

        var min = double.MaxValue;
        var max = double.MinValue;
        foreach (var value in values)
        {
            if (value < min) min = value;
            if (value > max) max = value;
        }

        var range = max - min;
        var sb = new StringBuilder(values.Length);
        foreach (var value in values)
        {
            var index = range <= 0 ? 0 : (int)Math.Round((value - min) / range * (SparkChars.Length - 1));
            sb.Append(SparkChars[Math.Clamp(index, 0, SparkChars.Length - 1)]);
        }

        return sb.ToString();
    }
}
