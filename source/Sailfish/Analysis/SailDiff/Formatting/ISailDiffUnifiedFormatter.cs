using System.Collections.Generic;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.SailDiff.Formatting;

/// <summary>
/// Unified formatter interface for all SailDiff output formats.
/// Provides context-adaptive formatting that combines visual impact summaries 
/// with detailed statistical tables for optimal user experience.
/// </summary>
public interface ISailDiffUnifiedFormatter
{
    /// <summary>
    /// Formats SailDiff comparison data for the specified output context.
    /// </summary>
    /// <param name="data">The comparison data to format</param>
    /// <param name="context">The target output context (IDE, Markdown, Console)</param>
    /// <returns>Formatted output optimized for the specified context</returns>
    SailDiffFormattedOutput Format(SailDiffComparisonData data, OutputContext context);

    /// <summary>
    /// Formats multiple SailDiff comparison results for the specified output context.
    /// </summary>
    /// <param name="comparisons">Collection of comparison data to format</param>
    /// <param name="context">The target output context</param>
    /// <returns>Formatted output with all comparisons</returns>
    SailDiffFormattedOutput FormatMultiple(IEnumerable<SailDiffComparisonData> comparisons, OutputContext context);
}

/// <summary>
/// Represents the target output context for formatting.
/// </summary>
public enum OutputContext
{
    /// <summary>
    /// IDE test output window - supports emojis, colors, and rich formatting
    /// </summary>
    IDE,

    /// <summary>
    /// Markdown files - GitHub-compatible markdown with tables
    /// </summary>
    Markdown,

    /// <summary>
    /// Console output - plain text with basic formatting
    /// </summary>
    Console,

    /// <summary>
    /// CSV export - structured data for analysis tools
    /// </summary>
    CSV
}

/// <summary>
/// Unified data model for SailDiff comparison information.
/// Abstracts the underlying statistical data for consistent formatting.
/// </summary>
public class SailDiffComparisonData
{
    /// <summary>
    /// Name of the comparison group (e.g., "SortingAlgorithms")
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the primary method being compared
    /// </summary>
    public string PrimaryMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the method being compared against
    /// </summary>
    public string ComparedMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Statistical test results from SailDiff analysis
    /// </summary>
    public StatisticalTestResult Statistics { get; set; } = null!;

    /// <summary>
    /// Additional metadata about the comparison
    /// </summary>
    public ComparisonMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Indicates if this comparison is from a specific method's perspective
    /// </summary>
    public bool IsPerspectiveBased { get; set; }

    /// <summary>
    /// The perspective method name if this is a perspective-based comparison
    /// </summary>
    public string? PerspectiveMethodName { get; set; }
}

/// <summary>
/// Additional metadata about a comparison that affects formatting.
/// </summary>
public class ComparisonMetadata
{
    /// <summary>
    /// Sample size for the comparison
    /// </summary>
    public int SampleSize { get; set; }

    /// <summary>
    /// Alpha level used for statistical significance testing
    /// </summary>
    public double AlphaLevel { get; set; } = 0.05;

    /// <summary>
    /// Type of statistical test performed
    /// </summary>
    public string TestType { get; set; } = "T-Test";

    /// <summary>
    /// Number of outliers removed from the analysis
    /// </summary>
    public int OutliersRemoved { get; set; }

    /// <summary>
    /// Additional context or notes about the comparison
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Container for formatted SailDiff output in different formats.
/// </summary>
public class SailDiffFormattedOutput
{
    /// <summary>
    /// Quick visual impact summary (e.g., "🔴 Performance: 99.7% slower (REGRESSED)")
    /// </summary>
    public string ImpactSummary { get; set; } = string.Empty;

    /// <summary>
    /// Detailed statistical table with all metrics
    /// </summary>
    public string DetailedTable { get; set; } = string.Empty;

    /// <summary>
    /// Complete formatted output combining impact summary and detailed table
    /// </summary>
    public string FullOutput { get; set; } = string.Empty;

    /// <summary>
    /// Statistical significance and direction of the comparison
    /// </summary>
    public ComparisonSignificance Significance { get; set; }

    /// <summary>
    /// Percentage change between the compared methods
    /// </summary>
    public double PercentageChange { get; set; }

    /// <summary>
    /// Whether the change is statistically significant
    /// </summary>
    public bool IsStatisticallySignificant { get; set; }
}

/// <summary>
/// Represents the statistical significance and direction of a performance comparison.
/// </summary>
public enum ComparisonSignificance
{
    /// <summary>
    /// Performance improved significantly (faster)
    /// </summary>
    Improved,

    /// <summary>
    /// Performance regressed significantly (slower)
    /// </summary>
    Regressed,

    /// <summary>
    /// No statistically significant change
    /// </summary>
    NoChange
}
