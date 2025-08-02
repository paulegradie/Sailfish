using System;
using System.Globalization;

namespace Sailfish.Analysis.SailDiff.Formatting;

/// <summary>
/// Interface for creating visual impact summaries from comparison data.
/// </summary>
public interface IImpactSummaryFormatter
{
    /// <summary>
    /// Creates a visual impact summary for the specified output context.
    /// </summary>
    /// <param name="data">Comparison data to summarize</param>
    /// <param name="context">Target output context</param>
    /// <returns>Formatted impact summary string</returns>
    string CreateImpactSummary(SailDiffComparisonData data, OutputContext context);
}

/// <summary>
/// Creates visual impact summaries that provide immediate insights into performance comparisons.
/// Adapts formatting based on output context (IDE with emojis, Markdown, Console).
/// </summary>
public class ImpactSummaryFormatter : IImpactSummaryFormatter
{
    /// <summary>
    /// Creates a visual impact summary for the specified output context.
    /// </summary>
    public string CreateImpactSummary(SailDiffComparisonData data, OutputContext context)
    {
        var analysis = AnalyzeComparison(data);
        
        return context switch
        {
            OutputContext.IDE => CreateIDEImpactSummary(data, analysis),
            OutputContext.Markdown => CreateMarkdownImpactSummary(data, analysis),
            OutputContext.Console => CreateConsoleImpactSummary(data, analysis),
            OutputContext.CSV => CreateCSVImpactSummary(data, analysis),
            _ => CreateConsoleImpactSummary(data, analysis)
        };
    }

    /// <summary>
    /// Analyzes comparison data to determine significance, direction, and magnitude.
    /// </summary>
    private ComparisonAnalysis AnalyzeComparison(SailDiffComparisonData data)
    {
        var stats = data.Statistics;
        
        // Calculate percentage change
        var primaryTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
            ? stats.MeanAfter 
            : stats.MeanBefore;
        var comparedTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
            ? stats.MeanBefore 
            : stats.MeanAfter;

        var percentChange = primaryTime > 0 ? ((comparedTime - primaryTime) / primaryTime) * 100 : 0;
        
        // Determine statistical significance
        var isSignificant = stats.PValue < data.Metadata.AlphaLevel &&
                           !stats.ChangeDescription.Contains("No Change", StringComparison.OrdinalIgnoreCase);

        // Determine significance category
        ComparisonSignificance significance;
        if (!isSignificant)
        {
            significance = ComparisonSignificance.NoChange;
        }
        else if (percentChange < 0)
        {
            significance = ComparisonSignificance.Improved; // Faster is better
        }
        else
        {
            significance = ComparisonSignificance.Regressed; // Slower is worse
        }

        return new ComparisonAnalysis
        {
            PercentageChange = Math.Abs(percentChange),
            IsImprovement = percentChange < 0,
            IsStatisticallySignificant = isSignificant,
            Significance = significance,
            PrimaryTime = primaryTime,
            ComparedTime = comparedTime,
            PValue = stats.PValue
        };
    }

    /// <summary>
    /// Creates impact summary for IDE output with emojis and rich formatting.
    /// </summary>
    private string CreateIDEImpactSummary(SailDiffComparisonData data, ComparisonAnalysis analysis)
    {
        var icon = GetSignificanceIcon(analysis.Significance);
        var direction = analysis.IsImprovement ? "faster" : "slower";
        var significanceText = GetSignificanceText(analysis.Significance);
        
        var summary = $"{icon} IMPACT: {data.PrimaryMethodName} vs {data.ComparedMethodName} - " +
                     $"{analysis.PercentageChange:F1}% {direction} ({significanceText})";

        if (analysis.IsStatisticallySignificant)
        {
            summary += $"\n   P-Value: {analysis.PValue:F6} | Mean: {analysis.PrimaryTime:F3}ms → {analysis.ComparedTime:F3}ms";
        }

        return summary;
    }

    /// <summary>
    /// Creates impact summary for Markdown output with GitHub-compatible formatting.
    /// </summary>
    private string CreateMarkdownImpactSummary(SailDiffComparisonData data, ComparisonAnalysis analysis)
    {
        var icon = GetMarkdownSignificanceIcon(analysis.Significance);
        var direction = analysis.IsImprovement ? "faster" : "slower";
        var significanceText = GetSignificanceText(analysis.Significance);
        
        return $"**{icon} IMPACT: {data.PrimaryMethodName} vs {data.ComparedMethodName} - " +
               $"{analysis.PercentageChange:F1}% {direction} ({significanceText})**";
    }

    /// <summary>
    /// Creates impact summary for console output with plain text formatting.
    /// </summary>
    private string CreateConsoleImpactSummary(SailDiffComparisonData data, ComparisonAnalysis analysis)
    {
        var indicator = GetConsoleSignificanceIndicator(analysis.Significance);
        var direction = analysis.IsImprovement ? "faster" : "slower";
        var significanceText = GetSignificanceText(analysis.Significance);
        
        return $"{indicator} IMPACT: {data.PrimaryMethodName} vs {data.ComparedMethodName} - " +
               $"{analysis.PercentageChange:F1}% {direction} ({significanceText})";
    }

    /// <summary>
    /// Creates impact summary for CSV output with structured data.
    /// </summary>
    private string CreateCSVImpactSummary(SailDiffComparisonData data, ComparisonAnalysis analysis)
    {
        var direction = analysis.IsImprovement ? "faster" : "slower";
        var significanceText = GetSignificanceText(analysis.Significance);
        
        return $"{data.PrimaryMethodName},{data.ComparedMethodName}," +
               $"{analysis.PercentageChange:F1}%,{direction},{significanceText}," +
               $"{analysis.PValue:F6},{analysis.PrimaryTime:F3},{analysis.ComparedTime:F3}";
    }

    /// <summary>
    /// Gets emoji icon for significance level (IDE output).
    /// </summary>
    private static string GetSignificanceIcon(ComparisonSignificance significance)
    {
        return significance switch
        {
            ComparisonSignificance.Improved => "🟢",
            ComparisonSignificance.Regressed => "🔴",
            ComparisonSignificance.NoChange => "⚪",
            _ => "⚪"
        };
    }

    /// <summary>
    /// Gets markdown-compatible icon for significance level.
    /// </summary>
    private static string GetMarkdownSignificanceIcon(ComparisonSignificance significance)
    {
        return significance switch
        {
            ComparisonSignificance.Improved => "🟢",
            ComparisonSignificance.Regressed => "🔴", 
            ComparisonSignificance.NoChange => "⚪",
            _ => "⚪"
        };
    }

    /// <summary>
    /// Gets console-compatible indicator for significance level.
    /// </summary>
    private static string GetConsoleSignificanceIndicator(ComparisonSignificance significance)
    {
        return significance switch
        {
            ComparisonSignificance.Improved => "[+]",
            ComparisonSignificance.Regressed => "[-]",
            ComparisonSignificance.NoChange => "[=]",
            _ => "[=]"
        };
    }

    /// <summary>
    /// Gets human-readable significance text.
    /// </summary>
    private static string GetSignificanceText(ComparisonSignificance significance)
    {
        return significance switch
        {
            ComparisonSignificance.Improved => "IMPROVED",
            ComparisonSignificance.Regressed => "REGRESSED",
            ComparisonSignificance.NoChange => "NO CHANGE",
            _ => "NO CHANGE"
        };
    }
}

/// <summary>
/// Internal analysis result for comparison data.
/// </summary>
internal class ComparisonAnalysis
{
    public double PercentageChange { get; set; }
    public bool IsImprovement { get; set; }
    public bool IsStatisticallySignificant { get; set; }
    public ComparisonSignificance Significance { get; set; }
    public double PrimaryTime { get; set; }
    public double ComparedTime { get; set; }
    public double PValue { get; set; }
}
