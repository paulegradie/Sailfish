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

    /// <summary>
    /// Enhanced variant that threads the user-configured <paramref name="alpha"/> through to the
    /// unified formatter, so the significance threshold visible in the output matches the
    /// threshold the test actually used. Prefer this overload when settings are reachable.
    /// </summary>
    /// <remarks>
    /// Default implementation falls back to <see cref="ConvertToEnhancedMarkdownTable(IEnumerable{SailDiffResult}, OutputContext)"/>
    /// so existing implementers do not need to update. Implementers that want to surface a
    /// non-default alpha should override this overload.
    /// </remarks>
    string ConvertToEnhancedMarkdownTable(IEnumerable<SailDiffResult> testCaseResults, OutputContext context, double alpha)
        => ConvertToEnhancedMarkdownTable(testCaseResults, context);
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
        => ConvertToEnhancedMarkdownTable(testCaseResults, context, Sailfish.Analysis.SailDiff.Statistics.SailDiffSignificance.FallbackAlpha);

    public string ConvertToEnhancedMarkdownTable(IEnumerable<SailDiffResult> testCaseResults, OutputContext context, double alpha)
    {
        var enumeratedResults = testCaseResults.ToList();

        if (!enumeratedResults.Any())
        {
            return "No SailDiff results available.";
        }

        // If unified formatter is available, use it for enhanced output
        if (_unifiedFormatter != null)
        {
            return CreateUnifiedFormattedOutput(enumeratedResults, context, alpha);
        }

        // Fallback to enhanced legacy format. Pass the configured alpha so the legacy impact
        // summary's significance decision honours the user setting — without this the legacy
        // path silently downgraded a real "Regressed" result to "NO CHANGE" for any p-value
        // between 0.05 and the configured alpha (e.g. α = 0.10 on the Relaxed preset).
        return CreateEnhancedLegacyOutput(enumeratedResults, context, alpha);
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
            // Tier-2 fields surface in the legacy markdown so consumers that still depend on
            // this format (and there are several) see the same magnitude story as the IDE
            // banner. Nullable values render as empty cells.
            m => FormatQValue(m.TestResultsWithOutlierAnalysis.StatisticalTestResult.QValue),
            m => FormatEffectSize(m.TestResultsWithOutlierAnalysis.StatisticalTestResult.EffectSize),
            m => FormatDifference(m.TestResultsWithOutlierAnalysis.StatisticalTestResult.Difference),
            m => FormatMde(m.TestResultsWithOutlierAnalysis.StatisticalTestResult.MinimumDetectableEffectPercent),
            m => m.TestResultsWithOutlierAnalysis.StatisticalTestResult.ChangeDescription
        };

        var headers = new List<string>
        {
            "Display Name",
            $"MeanBefore (N={nBefore})", $"MeanAfter (N={nAfter})",
            "MedianBefore", "MedianAfter",
            "PValue", "QValue (BH)", "Effect", "Shift", "MDE",
            "Change Description"
        };
        var columnValueSuffixes = new List<string>
        {
            "", "ms", "ms", "ms", "ms", "", "", "", "", "%", ""
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

    private static string FormatQValue(double? q) => q.HasValue ? q.Value.ToString("0.####") : string.Empty;

    private static string FormatEffectSize(Sailfish.Contracts.Public.Models.EffectSizeReport? effect)
    {
        if (effect is null) return string.Empty;
        // CI bounds use the same 0.### precision as the point estimate (and as FormatDifference)
        // so all bracket-style reports show consistent precision in console/markdown output.
        var ci = effect.CiLower.HasValue && effect.CiUpper.HasValue
            ? $" [{effect.CiLower.Value:0.###}, {effect.CiUpper.Value:0.###}]"
            : string.Empty;
        return $"{effect.Name}={effect.Value:0.###}{ci}";
    }

    private static string FormatDifference(Sailfish.Contracts.Public.Models.DifferenceReport? diff)
    {
        if (diff is null) return string.Empty;
        var ci = diff.CiLower.HasValue && diff.CiUpper.HasValue
            ? $" [{diff.CiLower.Value:0.###}, {diff.CiUpper.Value:0.###}]"
            : string.Empty;
        return $"{diff.Value:0.###} {diff.Units}{ci}";
    }

    private static string FormatMde(double? mde) => mde.HasValue ? mde.Value.ToString("0.##") : string.Empty;

    private string CreateUnifiedFormattedOutput(List<SailDiffResult> enumeratedResults, OutputContext context, double alpha)
    {
        var sb = new StringBuilder();

        // Add header for multiple results
        if (context == OutputContext.Markdown)
        {
            sb.AppendLine("## SailDiff Performance Analysis");
            sb.AppendLine();
        }

        // Convert each SailDiff result to comparison data and format
        var comparisons = enumeratedResults.Select(result => ConvertToComparisonData(result, alpha)).ToList();

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

    private string CreateEnhancedLegacyOutput(List<SailDiffResult> enumeratedResults, OutputContext context, double alpha)
    {
        var sb = new StringBuilder();

        // Add impact summaries for each result
        foreach (var result in enumeratedResults)
        {
            var impactSummary = CreateLegacyImpactSummary(result, context, alpha);
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

    private SailDiffComparisonData ConvertToComparisonData(SailDiffResult result, double alpha)
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
                AlphaLevel = alpha,
                TestType = "T-Test",
                OutliersRemoved = (result.TestResultsWithOutlierAnalysis.Sample1?.TotalNumOutliers ?? 0) +
                                 (result.TestResultsWithOutlierAnalysis.Sample2?.TotalNumOutliers ?? 0)
            },
            IsPerspectiveBased = false
        };
    }

    private string CreateLegacyImpactSummary(SailDiffResult result, OutputContext context, double alpha)
    {
        var stats = result.TestResultsWithOutlierAnalysis.StatisticalTestResult;
        var percentChange = stats.MeanBefore > 0 ? ((stats.MeanAfter - stats.MeanBefore) / stats.MeanBefore) * 100 : 0;

        // Prefer the test wrapper's already-computed ChangeDescription as the authoritative
        // significance signal — that value was produced by the test using the user's α, so
        // recomputing here would risk disagreeing with the rest of the pipeline. Only when
        // the wrapper failed to set a recognisable verdict do we fall back to p ≤ α.
        var hasVerdict = !string.IsNullOrEmpty(stats.ChangeDescription);
        bool isSignificant;
        bool isImprovement;
        if (hasVerdict)
        {
            isSignificant = !stats.ChangeDescription.Contains("No Change", StringComparison.OrdinalIgnoreCase);
            isImprovement = stats.ChangeDescription.Contains("Improved", StringComparison.OrdinalIgnoreCase)
                            || (isSignificant && percentChange < 0);
        }
        else
        {
            isSignificant = stats.PValue <= alpha;
            isImprovement = percentChange < 0;
        }

        if (!isSignificant)
        {
            var noChangeIcon = context == OutputContext.Console ? "[=]" : "⚪";
            return $"{noChangeIcon} **{result.TestCaseId.DisplayName}**: {Math.Abs(percentChange):F1}% difference (NO CHANGE)";
        }

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