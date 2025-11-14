using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        // Create table headers
        var headers = new[] { "Metric", "Primary Method", "Compared Method", "Change", "P-Value" };
        var rows = new List<string[]>();

        foreach (var data in comparisons)
        {
            var stats = data.Statistics;
            var primaryTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
                ? stats.MeanAfter 
                : stats.MeanBefore;
            var comparedTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
                ? stats.MeanBefore 
                : stats.MeanAfter;
            
            var percentChange = primaryTime > 0 ? ((comparedTime - primaryTime) / primaryTime) * 100 : 0;
            var changeText = percentChange >= 0 ? $"+{percentChange:F1}%" : $"{percentChange:F1}%";

            // Add comparison header if multiple comparisons
            if (comparisons.Count > 1)
            {
                sb.AppendLine($"Comparing: {data.PrimaryMethodName} vs {data.ComparedMethodName}");
            }

            rows.Add(new[] { "Mean", $"{primaryTime:F3}ms", $"{comparedTime:F3}ms", changeText, $"{stats.PValue:F6}" });
            
            var primaryMedian = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
                ? stats.MedianAfter 
                : stats.MedianBefore;
            var comparedMedian = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
                ? stats.MedianBefore 
                : stats.MedianAfter;
            
            var medianChange = primaryMedian > 0 ? ((comparedMedian - primaryMedian) / primaryMedian) * 100 : 0;
            var medianChangeText = medianChange >= 0 ? $"+{medianChange:F1}%" : $"{medianChange:F1}%";
            
            rows.Add(new[] { "Median", $"{primaryMedian:F3}ms", $"{comparedMedian:F3}ms", medianChangeText, "-" });

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

        foreach (var data in comparisons)
        {
            var stats = data.Statistics;
            var primaryTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
                ? stats.MeanAfter 
                : stats.MeanBefore;
            var comparedTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
                ? stats.MeanBefore 
                : stats.MeanAfter;
            
            var percentChange = primaryTime > 0 ? ((comparedTime - primaryTime) / primaryTime) * 100 : 0;
            var changeText = percentChange >= 0 ? $"+{percentChange:F1}%" : $"{percentChange:F1}%";

            sb.AppendLine();
            sb.AppendLine($"| Metric | {data.PrimaryMethodName} | {data.ComparedMethodName} | Change | P-Value |");
            sb.AppendLine("|--------|------------|-------------|--------|---------|");
            sb.AppendLine($"| Mean   | {primaryTime:F3}ms | {comparedTime:F3}ms | {changeText} | {stats.PValue:F6} |");
            
            var primaryMedian = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
                ? stats.MedianAfter 
                : stats.MedianBefore;
            var comparedMedian = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
                ? stats.MedianBefore 
                : stats.MedianAfter;
            
            var medianChange = primaryMedian > 0 ? ((comparedMedian - primaryMedian) / primaryMedian) * 100 : 0;
            var medianChangeText = medianChange >= 0 ? $"+{medianChange:F1}%" : $"{medianChange:F1}%";
            
            sb.AppendLine($"| Median | {primaryMedian:F3}ms | {comparedMedian:F3}ms | {medianChangeText} | - |");
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

        foreach (var data in comparisons)
        {
            var stats = data.Statistics;
            
            if (comparisons.Count > 1)
            {
                sb.AppendLine($"Comparing: {data.PrimaryMethodName} vs {data.ComparedMethodName}");
                sb.AppendLine(new string('-', 40));
            }

            var primaryTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
                ? stats.MeanAfter 
                : stats.MeanBefore;
            var comparedTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
                ? stats.MeanBefore 
                : stats.MeanAfter;

            sb.AppendLine($"Mean:     {primaryTime:F3}ms -> {comparedTime:F3}ms");
            sb.AppendLine($"P-Value:  {stats.PValue:F6}");
            sb.AppendLine($"Change:   {stats.ChangeDescription}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates a CSV format for data export and analysis.
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
