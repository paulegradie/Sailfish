using System;
using System.Text;

namespace Sailfish.Analysis.SailDiff.Formatting;

/// <summary>
/// Interface for adapting formatted output to specific contexts.
/// </summary>
public interface IOutputContextAdapter
{
    /// <summary>
    /// Adapts the impact summary and detailed table to the specified output context.
    /// </summary>
    /// <param name="impactSummary">Formatted impact summary</param>
    /// <param name="detailedTable">Formatted detailed table</param>
    /// <param name="context">Target output context</param>
    /// <param name="groupName">Optional group name for context</param>
    /// <returns>Complete formatted output for the context</returns>
    string AdaptToContext(string impactSummary, string detailedTable, OutputContext context, string? groupName = null);
}

/// <summary>
/// Adapts formatted SailDiff output to different contexts (IDE, Markdown, Console, CSV).
/// Handles context-specific formatting, headers, and layout requirements.
/// </summary>
public class OutputContextAdapter : IOutputContextAdapter
{
    /// <summary>
    /// Adapts the impact summary and detailed table to the specified output context.
    /// </summary>
    public string AdaptToContext(string impactSummary, string detailedTable, OutputContext context, string? groupName = null)
    {
        return context switch
        {
            OutputContext.IDE => AdaptForIDE(impactSummary, detailedTable, groupName),
            OutputContext.Markdown => AdaptForMarkdown(impactSummary, detailedTable, groupName),
            OutputContext.Console => AdaptForConsole(impactSummary, detailedTable, groupName),
            OutputContext.CSV => AdaptForCSV(impactSummary, detailedTable, groupName),
            _ => AdaptForConsole(impactSummary, detailedTable, groupName)
        };
    }

    /// <summary>
    /// Adapts output for IDE test output window with rich formatting and visual hierarchy.
    /// </summary>
    private string AdaptForIDE(string impactSummary, string detailedTable, string? groupName)
    {
        var sb = new StringBuilder();
        
        // Add header with visual separator
        sb.AppendLine();
        sb.AppendLine("📊 PERFORMANCE COMPARISON");
        
        if (!string.IsNullOrEmpty(groupName))
        {
            sb.AppendLine($"Group: {groupName}");
        }
        
        sb.AppendLine(new string('=', 50));
        sb.AppendLine();
        
        // Impact summary with emphasis
        sb.AppendLine(impactSummary);
        
        // Detailed table if available
        if (!string.IsNullOrEmpty(detailedTable))
        {
            sb.AppendLine();
            sb.Append(detailedTable);
        }
        
        // Footer separator
        sb.AppendLine();
        sb.AppendLine(new string('=', 50));
        
        return sb.ToString();
    }

    /// <summary>
    /// Adapts output for Markdown files with GitHub-compatible formatting.
    /// </summary>
    private string AdaptForMarkdown(string impactSummary, string detailedTable, string? groupName)
    {
        var sb = new StringBuilder();
        
        // Add markdown header
        if (!string.IsNullOrEmpty(groupName))
        {
            sb.AppendLine($"### {groupName} Performance Comparison");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("### Performance Comparison");
            sb.AppendLine();
        }
        
        // Impact summary as emphasized text
        sb.AppendLine(impactSummary);
        
        // Detailed table if available
        if (!string.IsNullOrEmpty(detailedTable))
        {
            sb.AppendLine();
            sb.Append(detailedTable);
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Adapts output for console with plain text formatting and clear structure.
    /// </summary>
    private string AdaptForConsole(string impactSummary, string detailedTable, string? groupName)
    {
        var sb = new StringBuilder();
        
        // Add console header
        sb.AppendLine();
        sb.AppendLine("PERFORMANCE COMPARISON");
        
        if (!string.IsNullOrEmpty(groupName))
        {
            sb.AppendLine($"Group: {groupName}");
        }
        
        sb.AppendLine(new string('=', 60));
        sb.AppendLine();
        
        // Impact summary
        sb.AppendLine(impactSummary);
        
        // Detailed table if available
        if (!string.IsNullOrEmpty(detailedTable))
        {
            sb.Append(detailedTable);
        }
        
        sb.AppendLine(new string('=', 60));
        
        return sb.ToString();
    }

    /// <summary>
    /// Adapts output for CSV export with structured data format.
    /// </summary>
    private string AdaptForCSV(string impactSummary, string detailedTable, string? groupName)
    {
        var sb = new StringBuilder();
        
        // Add CSV metadata header if group name is provided
        if (!string.IsNullOrEmpty(groupName))
        {
            sb.AppendLine($"# Group: {groupName}");
            sb.AppendLine($"# Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();
        }
        
        // For CSV, we primarily use the detailed table which should already be in CSV format
        if (!string.IsNullOrEmpty(detailedTable))
        {
            sb.Append(detailedTable);
        }
        else
        {
            // Fallback: convert impact summary to CSV format
            sb.AppendLine("Summary");
            sb.AppendLine($"\"{impactSummary}\"");
        }
        
        return sb.ToString();
    }
}
