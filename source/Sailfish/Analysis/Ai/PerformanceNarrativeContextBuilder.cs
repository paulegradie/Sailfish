using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;

namespace Sailfish.Analysis.Ai;

internal interface IPerformanceNarrativeContextBuilder
{
    PerformanceNarrativeContext Build(SailDiffAnalysisCompleteNotification notification, double alpha);
}

/// <summary>
///     Lifts the authoritative SailDiff figures into the grounded packet the agent reasons over. The verdict is
///     derived here (deterministically, from the p-value and the direction of the mean shift) so the agent never
///     has to — and so the same vocabulary is used everywhere.
/// </summary>
internal sealed class PerformanceNarrativeContextBuilder : IPerformanceNarrativeContextBuilder
{
    public PerformanceNarrativeContext Build(SailDiffAnalysisCompleteNotification notification, double alpha)
    {
        var comparisons = notification.TestCaseResults
            .Select(result => ToCaseContext(result, alpha))
            .ToList();

        return new PerformanceNarrativeContext(comparisons, notification.ResultsAsMarkdown ?? string.Empty);
    }

    private static SailDiffCaseContext ToCaseContext(SailDiffResult result, double alpha)
    {
        var displayName = result.TestCaseId.DisplayName;
        var stats = result.TestResultsWithOutlierAnalysis.StatisticalTestResult;

        if (stats.Failed)
        {
            return new SailDiffCaseContext(
                displayName, SkipperVerdict.Inconclusive,
                MeanBefore: 0, MeanAfter: 0, MedianBefore: 0, MedianAfter: 0,
                PercentChangeMean: 0, PValue: double.NaN, AdjustedPValue: null,
                ChangeDescription: stats.ChangeDescription, SampleSizeBefore: 0, SampleSizeAfter: 0, Failed: true);
        }

        var percentChangeMean = stats.MeanBefore != 0
            ? (stats.MeanAfter - stats.MeanBefore) / stats.MeanBefore * 100.0
            : 0.0;

        return new SailDiffCaseContext(
            displayName,
            DeriveVerdict(stats, alpha),
            stats.MeanBefore,
            stats.MeanAfter,
            stats.MedianBefore,
            stats.MedianAfter,
            percentChangeMean,
            stats.PValue,
            stats.QValue,
            stats.ChangeDescription,
            stats.SampleSizeBefore,
            stats.SampleSizeAfter,
            Failed: false);
    }

    private static SkipperVerdict DeriveVerdict(StatisticalTestResult stats, double alpha)
    {
        // Prefer the BH-FDR adjusted q-value when present (it controls the family-wise error rate across the
        // pairs in an N×N method comparison); otherwise fall back to the raw p-value.
        var p = stats.QValue ?? stats.PValue;
        if (double.IsNaN(p) || p >= alpha) return SkipperVerdict.NotSignificant;

        return stats.MeanAfter > stats.MeanBefore ? SkipperVerdict.Regressed : SkipperVerdict.Improved;
    }
}
