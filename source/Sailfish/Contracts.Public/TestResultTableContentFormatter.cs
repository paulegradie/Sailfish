using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Methods;

namespace Sailfish.Contracts.Public;

public interface ISailDiffResultMarkdownConverter
{
    string ConvertToMarkdownTable(IEnumerable<SailDiffResult> testCaseResults);
    string ConvertToEnhancedMarkdownTable(IEnumerable<SailDiffResult> testCaseResults, OutputContext context = OutputContext.Markdown);
}

public class SailDiffResultMarkdownConverter : ISailDiffResultMarkdownConverter
{
    private readonly ISailDiffUnifiedFormatter? _unifiedFormatter;

    public SailDiffResultMarkdownConverter()
    {
        // Default constructor for backward compatibility
        _unifiedFormatter = null;
    }

    public SailDiffResultMarkdownConverter(ISailDiffUnifiedFormatter unifiedFormatter)
    {
        _unifiedFormatter = unifiedFormatter ?? throw new ArgumentNullException(nameof(unifiedFormatter));
    }

    public string ConvertToMarkdownTable(IEnumerable<SailDiffResult> testCaseResults)
    {
        // Maintain backward compatibility with existing table format
        return CreateLegacyMarkdownTable(testCaseResults);
    }

    public string ConvertToEnhancedMarkdownTable(IEnumerable<SailDiffResult> testCaseResults, OutputContext context = OutputContext.Markdown)
    {
        var enumeratedResults = testCaseResults.ToList();

        if (!enumeratedResults.Any())
        {
            return "No SailDiff results available.";
        }

        // If unified formatter is available, use it for enhanced output
        if (_unifiedFormatter != null)
        {
            return CreateUnifiedFormattedOutput(enumeratedResults, context);
        }

        // Fallback to enhanced legacy format
        return CreateEnhancedLegacyOutput(enumeratedResults, context);
    }

    private string CreateLegacyMarkdownTable(IEnumerable<SailDiffResult> testCaseResults)
    {
        var enumeratedResults = testCaseResults.ToList();
        if (!enumeratedResults.Any()) return string.Empty;

        var nBefore = enumeratedResults.Select(x => x.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeBefore).Distinct().Single();
        var nAfter = enumeratedResults.Select(x => x.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeAfter).Distinct().Single();

        var selectors = new List<Expression<Func<SailDiffResult, object>>>
        {
            m => m.TestCaseId.DisplayName,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanBefore,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanAfter,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianBefore,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianAfter,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue,
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.ChangeDescription
        };

        var headers = new List<string>
        {
            "Display Name", $"MeanBefore (N={nBefore})", $"MeanAfter (N={nAfter})", "MedianBefore", "MedianAfter", "PValue", "Change Description"
        };
        var columnValueSuffixes = new List<string>
        {
            "", "ms", "ms", "ms", "ms", "", ""
        };

        if (enumeratedResults.Any(x => !string.IsNullOrEmpty(x.TestResultsWithOutlierAnalysis.ExceptionMessage)))
        {
            selectors.Add(m => m.TestResultsWithOutlierAnalysis.ExceptionMessage);
            headers.Add("Exception");
            columnValueSuffixes.Add("");
        }

        return enumeratedResults.ToStringTable(
            columnValueSuffixes,
            headers,
            [.. selectors]);
    }

    private string CreateUnifiedFormattedOutput(List<SailDiffResult> enumeratedResults, OutputContext context)
    {
        var sb = new StringBuilder();

        // Add header for multiple results
        if (context == OutputContext.Markdown)
        {
            sb.AppendLine("## SailDiff Performance Analysis");
            sb.AppendLine();
        }

        // Convert each SailDiff result to comparison data and format
        var comparisons = enumeratedResults.Select(result => ConvertToComparisonData(result)).ToList();

        if (comparisons.Count == 1)
        {
            // Single comparison
            var formatted = _unifiedFormatter!.Format(comparisons.First(), context);
            sb.Append(formatted.FullOutput);
        }
        else
        {
            // Multiple comparisons
            var formatted = _unifiedFormatter!.FormatMultiple(comparisons, context);
            sb.Append(formatted.FullOutput);
        }

        return sb.ToString();
    }

    private string CreateEnhancedLegacyOutput(List<SailDiffResult> enumeratedResults, OutputContext context)
    {
        var sb = new StringBuilder();

        // Add impact summaries for each result
        foreach (var result in enumeratedResults)
        {
            var impactSummary = CreateLegacyImpactSummary(result, context);
            if (!string.IsNullOrEmpty(impactSummary))
            {
                sb.AppendLine(impactSummary);
                sb.AppendLine();
            }
        }

        // Add the traditional table
        var table = CreateLegacyMarkdownTable(enumeratedResults);
        sb.Append(table);

        return sb.ToString();
    }

    private SailDiffComparisonData ConvertToComparisonData(SailDiffResult result)
    {
        return new SailDiffComparisonData
        {
            GroupName = "SailDiff Analysis",
            PrimaryMethodName = "Before",
            ComparedMethodName = "After",
            Statistics = result.TestResultsWithOutlierAnalysis.StatisticalTestResult,
            Metadata = new ComparisonMetadata
            {
                SampleSize = result.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeBefore,
                AlphaLevel = 0.05,
                TestType = "T-Test",
                OutliersRemoved = (result.TestResultsWithOutlierAnalysis.Sample1?.TotalNumOutliers ?? 0) +
                                 (result.TestResultsWithOutlierAnalysis.Sample2?.TotalNumOutliers ?? 0)
            },
            IsPerspectiveBased = false
        };
    }

    private string CreateLegacyImpactSummary(SailDiffResult result, OutputContext context)
    {
        var stats = result.TestResultsWithOutlierAnalysis.StatisticalTestResult;
        var percentChange = stats.MeanBefore > 0 ? ((stats.MeanAfter - stats.MeanBefore) / stats.MeanBefore) * 100 : 0;
        var isSignificant = stats.PValue < 0.05 && !stats.ChangeDescription.Contains("No Change", StringComparison.OrdinalIgnoreCase);

        if (!isSignificant)
        {
            var noChangeIcon = context == OutputContext.Console ? "[=]" : "⚪";
            return $"{noChangeIcon} **{result.TestCaseId.DisplayName}**: {Math.Abs(percentChange):F1}% difference (NO CHANGE)";
        }

        var isImprovement = percentChange < 0;
        var direction = isImprovement ? "faster" : "slower";
        var significance = isImprovement ? "IMPROVED" : "REGRESSED";
        var icon = context switch
        {
            OutputContext.Console => isImprovement ? "[+]" : "[-]",
            _ => isImprovement ? "🟢" : "🔴"
        };

        return $"{icon} **{result.TestCaseId.DisplayName}**: {Math.Abs(percentChange):F1}% {direction} ({significance})";
    }
}