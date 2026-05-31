using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sailfish.Presentation;

namespace Sailfish.Analysis.SailDiff.Formatting;

/// <summary>
/// Interface for creating detailed statistical tables from comparison data.
/// </summary>
public interface IDetailedTableFormatter
{
    /// <summary>
    /// Creates a detailed statistical table for the specified output context.
    /// </summary>
    /// <param name="data">Comparison data to format</param>
    /// <param name="context">Target output context</param>
    /// <returns>Formatted statistical table</returns>
    string CreateDetailedTable(SailDiffComparisonData data, OutputContext context);

    /// <summary>
    /// Creates a detailed statistical table for multiple comparisons.
    /// </summary>
    /// <param name="comparisons">Collection of comparison data</param>
    /// <param name="context">Target output context</param>
    /// <returns>Formatted statistical table with all comparisons</returns>
    string CreateDetailedTable(IEnumerable<SailDiffComparisonData> comparisons, OutputContext context);
}

/// <summary>
/// Creates detailed statistical tables that provide comprehensive analysis data.
/// Maintains scientific rigor while adapting format to output context.
/// </summary>
public class DetailedTableFormatter : IDetailedTableFormatter
{
    // Decimals to show within the auto-selected time unit (e.g. "1.100 µs").
    private const int Decimals = 3;

    /// <summary>
    /// Creates a detailed statistical table for a single comparison.
    /// </summary>
    public string CreateDetailedTable(SailDiffComparisonData data, OutputContext context)
    {
        return CreateDetailedTable(new[] { data }, context);
    }

    /// <summary>
    /// Creates a detailed statistical table for multiple comparisons.
    /// </summary>
    public string CreateDetailedTable(IEnumerable<SailDiffComparisonData> comparisons, OutputContext context)
    {
        var comparisonList = comparisons.ToList();
        if (!comparisonList.Any())
        {
            return string.Empty;
        }

        return context switch
        {
            OutputContext.Ide => CreateIdeTable(comparisonList),
            OutputContext.Markdown => CreateMarkdownTable(comparisonList),
            OutputContext.Console => CreateConsoleTable(comparisonList),
            OutputContext.Csv => CreateCsvTable(comparisonList),
            _ => CreateConsoleTable(comparisonList)
        };
    }

    /// <summary>
    /// Creates a table formatted for IDE output with enhanced readability.
    /// </summary>
    private string CreateIdeTable(List<SailDiffComparisonData> comparisons)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("📋 DETAILED STATISTICS:");
        sb.AppendLine();

        // One magnitude-appropriate unit for the whole table so fast methods aren't shown as 0.000ms.
        var unit = SelectUnit(comparisons);
        var unitLabel = DurationFormatter.UnitLabel(unit);

        // Create table headers
        var headers = new[] { "Metric", "Primary Method", "Compared Method", "Change", "P-Value" };
        var rows = new List<string[]>();

        foreach (var data in comparisons)
        {
            var stats = data.Statistics;
            var display = SailDiffDisplayStatistics.From(stats);
            var swap = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName;
            var primaryTime = swap ? display.MeanAfter : display.MeanBefore;
            var comparedTime = swap ? display.MeanBefore : display.MeanAfter;

            var percentChange = primaryTime > 0 ? ((comparedTime - primaryTime) / primaryTime) * 100 : 0;
            var changeText = percentChange >= 0 ? $"+{percentChange:F1}%" : $"{percentChange:F1}%";

            // Add comparison header if multiple comparisons
            if (comparisons.Count > 1)
            {
                sb.AppendLine($"Comparing: {data.PrimaryMethodName} vs {data.ComparedMethodName}");
            }

            rows.Add(new[] { $"Mean ({unitLabel})", DurationFormatter.Format(primaryTime, unit, Decimals), DurationFormatter.Format(comparedTime, unit, Decimals), changeText, $"{stats.PValue:F6}" });

            var primaryMedian = swap ? display.MedianAfter : display.MedianBefore;
            var comparedMedian = swap ? display.MedianBefore : display.MedianAfter;

            var medianChange = primaryMedian > 0 ? ((comparedMedian - primaryMedian) / primaryMedian) * 100 : 0;
            var medianChangeText = medianChange >= 0 ? $"+{medianChange:F1}%" : $"{medianChange:F1}%";

            rows.Add(new[] { $"Median ({unitLabel})", DurationFormatter.Format(primaryMedian, unit, Decimals), DurationFormatter.Format(comparedMedian, unit, Decimals), medianChangeText, "-" });

            if (comparisons.Count > 1)
            {
                sb.AppendLine();
            }
        }

        // Format as aligned table
        sb.Append(FormatAsAlignedTable(headers, rows));

        // Add metadata
        if (comparisons.Any())
        {
            var firstComparison = comparisons.First();
            sb.AppendLine();
            sb.AppendLine($"Statistical Test: {firstComparison.Metadata.TestType}");
            sb.AppendLine($"Alpha Level: {firstComparison.Metadata.AlphaLevel}");
            sb.AppendLine($"Sample Size: {firstComparison.Metadata.SampleSize}");

            if (firstComparison.Metadata.OutliersRemoved > 0)
            {
                sb.AppendLine($"Outliers Removed: {firstComparison.Metadata.OutliersRemoved}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates a markdown table compatible with GitHub rendering.
    /// </summary>
    private string CreateMarkdownTable(List<SailDiffComparisonData> comparisons)
    {
        var sb = new StringBuilder();

        var unit = SelectUnit(comparisons);
        var unitLabel = DurationFormatter.UnitLabel(unit);

        foreach (var data in comparisons)
        {
            var stats = data.Statistics;
            var display = SailDiffDisplayStatistics.From(stats);
            var swap = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName;
            var primaryTime = swap ? display.MeanAfter : display.MeanBefore;
            var comparedTime = swap ? display.MeanBefore : display.MeanAfter;

            var percentChange = primaryTime > 0 ? ((comparedTime - primaryTime) / primaryTime) * 100 : 0;
            var changeText = percentChange >= 0 ? $"+{percentChange:F1}%" : $"{percentChange:F1}%";

            sb.AppendLine();
            sb.AppendLine($"| Metric | {data.PrimaryMethodName} | {data.ComparedMethodName} | Change | P-Value |");
            sb.AppendLine("|--------|------------|-------------|--------|---------|");
            sb.AppendLine($"| Mean ({unitLabel}) | {DurationFormatter.Format(primaryTime, unit, Decimals)} | {DurationFormatter.Format(comparedTime, unit, Decimals)} | {changeText} | {stats.PValue:F6} |");

            var primaryMedian = swap ? display.MedianAfter : display.MedianBefore;
            var comparedMedian = swap ? display.MedianBefore : display.MedianAfter;

            var medianChange = primaryMedian > 0 ? ((comparedMedian - primaryMedian) / primaryMedian) * 100 : 0;
            var medianChangeText = medianChange >= 0 ? $"+{medianChange:F1}%" : $"{medianChange:F1}%";

            sb.AppendLine($"| Median ({unitLabel}) | {DurationFormatter.Format(primaryMedian, unit, Decimals)} | {DurationFormatter.Format(comparedMedian, unit, Decimals)} | {medianChangeText} | - |");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates a console table with plain text formatting.
    /// </summary>
    private string CreateConsoleTable(List<SailDiffComparisonData> comparisons)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("DETAILED STATISTICS:");
        sb.AppendLine(new string('=', 50));

        var unit = SelectUnit(comparisons);

        foreach (var data in comparisons)
        {
            var stats = data.Statistics;
            var display = SailDiffDisplayStatistics.From(stats);

            if (comparisons.Count > 1)
            {
                sb.AppendLine($"Comparing: {data.PrimaryMethodName} vs {data.ComparedMethodName}");
                sb.AppendLine(new string('-', 40));
            }

            var swap = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName;
            var primaryTime = swap ? display.MeanAfter : display.MeanBefore;
            var comparedTime = swap ? display.MeanBefore : display.MeanAfter;

            sb.AppendLine($"Mean:     {DurationFormatter.FormatWithUnit(primaryTime, unit, Decimals)} -> {DurationFormatter.FormatWithUnit(comparedTime, unit, Decimals)}");
            sb.AppendLine($"P-Value:  {stats.PValue:F6}");
            sb.AppendLine($"Change:   {stats.ChangeDescription}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates a CSV format for data export and analysis. Values stay in raw milliseconds for
    /// machine consumption — auto-scaling is a display concern only.
    /// </summary>
    private string CreateCsvTable(List<SailDiffComparisonData> comparisons)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PrimaryMethod,ComparedMethod,PrimaryMean,ComparedMean,PrimaryMedian,ComparedMedian,PValue,ChangeDescription,SampleSize");

        foreach (var data in comparisons)
        {
            var stats = data.Statistics;
            var primaryTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName
                ? stats.MeanAfter
                : stats.MeanBefore;
            var comparedTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName
                ? stats.MeanBefore
                : stats.MeanAfter;
            var primaryMedian = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName
                ? stats.MedianAfter
                : stats.MedianBefore;
            var comparedMedian = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName
                ? stats.MedianBefore
                : stats.MedianAfter;

            sb.AppendLine($"{data.PrimaryMethodName},{data.ComparedMethodName}," +
                         $"{primaryTime:F3},{comparedTime:F3}," +
                         $"{primaryMedian:F3},{comparedMedian:F3}," +
                         $"{stats.PValue:F6},{stats.ChangeDescription}," +
                         $"{data.Metadata.SampleSize}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Selects one display unit for a whole table from the full-precision means and medians of all
    /// comparisons, so columns share a unit and stay aligned (unit is carried in the row label).
    /// </summary>
    private static DurationUnit SelectUnit(List<SailDiffComparisonData> comparisons)
    {
        var values = new List<double>();
        foreach (var data in comparisons)
        {
            var display = SailDiffDisplayStatistics.From(data.Statistics);
            values.Add(display.MeanBefore);
            values.Add(display.MeanAfter);
            values.Add(display.MedianBefore);
            values.Add(display.MedianAfter);
        }

        return DurationFormatter.SelectUnit(values);
    }

    /// <summary>
    /// Formats data as an aligned table for console/IDE output.
    /// </summary>
    private static string FormatAsAlignedTable(string[] headers, List<string[]> rows)
    {
        if (!rows.Any()) return string.Empty;

        // Calculate column widths
        var columnWidths = new int[headers.Length];
        for (var i = 0; i < headers.Length; i++)
        {
            columnWidths[i] = Math.Max(headers[i].Length, rows.Max(row => row[i].Length));
        }

        var sb = new StringBuilder();

        // Header row
        sb.Append("| ");
        for (var i = 0; i < headers.Length; i++)
        {
            sb.Append(headers[i].PadRight(columnWidths[i]));
            sb.Append(" | ");
        }
        sb.AppendLine();

        // Separator row
        sb.Append("| ");
        for (var i = 0; i < headers.Length; i++)
        {
            sb.Append(new string('-', columnWidths[i]));
            sb.Append(" | ");
        }
        sb.AppendLine();

        // Data rows
        foreach (var row in rows)
        {
            sb.Append("| ");
            for (var i = 0; i < row.Length; i++)
            {
                sb.Append(row[i].PadRight(columnWidths[i]));
                sb.Append(" | ");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
