using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Analysis.SailDiff.Formatting;

/// <summary>
/// Unified formatter for all SailDiff output formats.
/// Provides context-adaptive formatting that combines visual impact summaries 
/// with detailed statistical tables for optimal user experience.
/// </summary>
public class SailDiffUnifiedFormatter : ISailDiffUnifiedFormatter
{
    private readonly IImpactSummaryFormatter _impactFormatter;
    private readonly IDetailedTableFormatter _tableFormatter;
    private readonly IOutputContextAdapter _contextAdapter;

    /// <summary>
    /// Initializes a new instance of the SailDiffUnifiedFormatter.
    /// </summary>
    /// <param name="impactFormatter">Formatter for creating impact summaries</param>
    /// <param name="tableFormatter">Formatter for creating detailed tables</param>
    /// <param name="contextAdapter">Adapter for context-specific formatting</param>
    public SailDiffUnifiedFormatter(
        IImpactSummaryFormatter impactFormatter,
        IDetailedTableFormatter tableFormatter,
        IOutputContextAdapter contextAdapter)
    {
        _impactFormatter = impactFormatter ?? throw new ArgumentNullException(nameof(impactFormatter));
        _tableFormatter = tableFormatter ?? throw new ArgumentNullException(nameof(tableFormatter));
        _contextAdapter = contextAdapter ?? throw new ArgumentNullException(nameof(contextAdapter));
    }

    /// <summary>
    /// Formats SailDiff comparison data for the specified output context.
    /// </summary>
    public SailDiffFormattedOutput Format(SailDiffComparisonData data, OutputContext context)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        // Generate impact summary
        var impactSummary = _impactFormatter.CreateImpactSummary(data, context);
        
        // Generate detailed table
        var detailedTable = _tableFormatter.CreateDetailedTable(data, context);
        
        // Adapt to context
        var fullOutput = _contextAdapter.AdaptToContext(impactSummary, detailedTable, context, data.GroupName);
        
        // Analyze significance
        var significance = DetermineSignificance(data.Statistics, data.Metadata.AlphaLevel);
        var percentageChange = CalculatePercentageChange(data);
        var isSignificant = IsStatisticallySignificant(data.Statistics, data.Metadata.AlphaLevel);

        return new SailDiffFormattedOutput
        {
            ImpactSummary = impactSummary,
            DetailedTable = detailedTable,
            FullOutput = fullOutput,
            Significance = significance,
            PercentageChange = Math.Abs(percentageChange),
            IsStatisticallySignificant = isSignificant
        };
    }

    /// <summary>
    /// Formats multiple SailDiff comparison results for the specified output context.
    /// </summary>
    public SailDiffFormattedOutput FormatMultiple(IEnumerable<SailDiffComparisonData> comparisons, OutputContext context)
    {
        var comparisonList = comparisons?.ToList() ?? throw new ArgumentNullException(nameof(comparisons));
        
        if (!comparisonList.Any())
        {
            return new SailDiffFormattedOutput
            {
                ImpactSummary = "No comparisons available",
                DetailedTable = string.Empty,
                FullOutput = "No comparisons available",
                Significance = ComparisonSignificance.NoChange,
                PercentageChange = 0,
                IsStatisticallySignificant = false
            };
        }

        // For multiple comparisons, we'll create a combined output
        var impactSummaries = comparisonList
            .Select(data => _impactFormatter.CreateImpactSummary(data, context))
            .ToList();

        // Create combined detailed table
        var detailedTable = _tableFormatter.CreateDetailedTable(comparisonList, context);

        // Combine impact summaries based on context
        var combinedImpactSummary = CombineImpactSummaries(impactSummaries, context);

        // Use the first comparison's group name for context
        var groupName = comparisonList.First().GroupName;
        var fullOutput = _contextAdapter.AdaptToContext(combinedImpactSummary, detailedTable, context, groupName);

        // Determine overall significance (most significant change)
        var mostSignificantComparison = comparisonList
            .OrderByDescending(data => Math.Abs(CalculatePercentageChange(data)))
            .First();

        var overallSignificance = DetermineSignificance(mostSignificantComparison.Statistics, mostSignificantComparison.Metadata.AlphaLevel);
        var overallPercentageChange = Math.Abs(CalculatePercentageChange(mostSignificantComparison));
        var overallIsSignificant = comparisonList.Any(data => IsStatisticallySignificant(data.Statistics, data.Metadata.AlphaLevel));

        return new SailDiffFormattedOutput
        {
            ImpactSummary = combinedImpactSummary,
            DetailedTable = detailedTable,
            FullOutput = fullOutput,
            Significance = overallSignificance,
            PercentageChange = overallPercentageChange,
            IsStatisticallySignificant = overallIsSignificant
        };
    }

    /// <summary>
    /// Determines the statistical significance and direction of a comparison.
    /// </summary>
    private static ComparisonSignificance DetermineSignificance(StatisticalTestResult statistics, double alphaLevel)
    {
        var isSignificant = statistics.PValue < alphaLevel &&
                           !statistics.ChangeDescription.Contains("No Change", StringComparison.OrdinalIgnoreCase);

        if (!isSignificant)
        {
            return ComparisonSignificance.NoChange;
        }

        // Determine if it's an improvement (faster) or regression (slower)
        // This is based on the change description or mean comparison
        var meanChange = statistics.MeanAfter - statistics.MeanBefore;
        
        return meanChange < 0 ? ComparisonSignificance.Improved : ComparisonSignificance.Regressed;
    }

    /// <summary>
    /// Calculates the percentage change between before and after measurements.
    /// </summary>
    private static double CalculatePercentageChange(SailDiffComparisonData data)
    {
        var stats = data.Statistics;
        
        // Handle perspective-based comparisons
        var primaryTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
            ? stats.MeanAfter 
            : stats.MeanBefore;
        var comparedTime = data.IsPerspectiveBased && data.PerspectiveMethodName == data.ComparedMethodName 
            ? stats.MeanBefore 
            : stats.MeanAfter;

        return primaryTime > 0 ? ((comparedTime - primaryTime) / primaryTime) * 100 : 0;
    }

    /// <summary>
    /// Determines if a statistical test result is statistically significant.
    /// </summary>
    private static bool IsStatisticallySignificant(StatisticalTestResult statistics, double alphaLevel)
    {
        return statistics.PValue < alphaLevel &&
               !statistics.ChangeDescription.Contains("No Change", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Combines multiple impact summaries based on the output context.
    /// </summary>
    private static string CombineImpactSummaries(List<string> impactSummaries, OutputContext context)
    {
        if (!impactSummaries.Any())
            return string.Empty;

        if (impactSummaries.Count == 1)
            return impactSummaries.First();

        return context switch
        {
            OutputContext.IDE => string.Join("\n\n", impactSummaries),
            OutputContext.Markdown => string.Join("\n\n", impactSummaries),
            OutputContext.Console => string.Join("\n\n", impactSummaries),
            OutputContext.CSV => string.Join("\n", impactSummaries),
            _ => string.Join("\n\n", impactSummaries)
        };
    }
}

/// <summary>
/// Factory for creating SailDiffUnifiedFormatter instances with default dependencies.
/// </summary>
public static class SailDiffUnifiedFormatterFactory
{
    /// <summary>
    /// Creates a new SailDiffUnifiedFormatter with default implementations.
    /// </summary>
    /// <returns>Configured SailDiffUnifiedFormatter instance</returns>
    public static ISailDiffUnifiedFormatter Create()
    {
        var impactFormatter = new ImpactSummaryFormatter();
        var tableFormatter = new DetailedTableFormatter();
        var contextAdapter = new OutputContextAdapter();

        return new SailDiffUnifiedFormatter(impactFormatter, tableFormatter, contextAdapter);
    }
}

/// <summary>
/// Extension methods for converting existing SailDiff data to the unified format.
/// </summary>
public static class SailDiffDataExtensions
{
    /// <summary>
    /// Converts a SailDiffResult to SailDiffComparisonData for unified formatting.
    /// </summary>
    /// <param name="result">The SailDiffResult to convert</param>
    /// <param name="groupName">Optional group name for the comparison</param>
    /// <param name="primaryMethodName">Name of the primary method</param>
    /// <param name="comparedMethodName">Name of the compared method</param>
    /// <returns>SailDiffComparisonData for unified formatting</returns>
    public static SailDiffComparisonData ToComparisonData(
        this SailDiffResult result,
        string groupName = "",
        string primaryMethodName = "",
        string comparedMethodName = "")
    {
        return new SailDiffComparisonData
        {
            GroupName = groupName,
            PrimaryMethodName = primaryMethodName,
            ComparedMethodName = comparedMethodName,
            Statistics = result.TestResultsWithOutlierAnalysis.StatisticalTestResult,
            Metadata = new ComparisonMetadata
            {
                SampleSize = result.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeBefore,
                AlphaLevel = 0.05, // Default alpha level
                TestType = "T-Test", // Default test type
                OutliersRemoved = (result.TestResultsWithOutlierAnalysis.Sample1?.TotalNumOutliers ?? 0) +
                                 (result.TestResultsWithOutlierAnalysis.Sample2?.TotalNumOutliers ?? 0)
            },
            IsPerspectiveBased = false
        };
    }
}
