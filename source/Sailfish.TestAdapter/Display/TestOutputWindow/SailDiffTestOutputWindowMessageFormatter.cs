using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Methods;

namespace Sailfish.TestAdapter.Display.TestOutputWindow;

internal interface ISailDiffTestOutputWindowMessageFormatter
{
    string FormTestOutputWindowMessageForSailDiff(
        SailDiffResult sailDiffResult,
        TestIds testIds,
        SailDiffSettings sailDiffSettings);
}

internal class SailDiffTestOutputWindowMessageFormatter : ISailDiffTestOutputWindowMessageFormatter
{
    public string FormTestOutputWindowMessageForSailDiff(
        SailDiffResult sailDiffResult,
        TestIds testIds,
        SailDiffSettings sailDiffSettings)
    {
        var stringBuilder = new StringBuilder();
        return StatisticalTestFailsThenBailAndReturnNothing(sailDiffResult, stringBuilder, out var errorDetails)
            ? FormattedSailDiffResult(testIds, sailDiffResult, sailDiffSettings, stringBuilder)
            : errorDetails;
    }

    private static bool StatisticalTestFailsThenBailAndReturnNothing(
        SailDiffResult sailDiffResult,
        StringBuilder stringBuilder,
        out string errorDetails)
    {
        errorDetails = string.Empty;
        if (StatTestResultIsOkayToPresent(sailDiffResult)) return true;

        stringBuilder.AppendLine("Statistical testing failed:");
        stringBuilder.AppendLine(sailDiffResult.TestResultsWithOutlierAnalysis.ExceptionMessage);
        errorDetails = stringBuilder.ToString();
        return false;
    }

    private static bool StatTestResultIsOkayToPresent(SailDiffResult sailDiffResult)
    {
        return !sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.Failed;
    }

    private static string FormattedSailDiffResult(TestIds testIds, SailDiffResult sailDiffResult, SailDiffSettings sailDiffSettings,
        StringBuilder stringBuilder)
    {
        // Add enhanced impact summary at the top
        var impactSummary = CreateImpactSummary(sailDiffResult, sailDiffSettings);
        if (!string.IsNullOrEmpty(impactSummary))
        {
            stringBuilder.AppendLine("📊 SAILDIFF PERFORMANCE ANALYSIS");
            stringBuilder.AppendLine(new string('=', 50));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(impactSummary);
            stringBuilder.AppendLine();
        }

        stringBuilder.AppendLine($"Before Ids: {string.Join(", ", testIds.BeforeTestIds)}");
        stringBuilder.AppendLine($"After Ids: {string.Join(", ", testIds.AfterTestIds)}");

        const string testLine = "📋 Statistical Test Details";
        stringBuilder.AppendLine(testLine);
        stringBuilder.AppendLine(string.Join("", Enumerable.Range(0, testLine.Length - 3).Select(x => "-"))); // -3 for emoji

        stringBuilder.AppendLine("Test Used:       " + sailDiffSettings.TestType);
        stringBuilder.AppendLine("PVal Threshold:  " + sailDiffSettings.Alpha);
        stringBuilder.AppendLine("PValue:          " +
                                 sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue);
        var significant = sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue <
                          sailDiffSettings.Alpha;
        var changeLine = "Change:          "
                         + sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.ChangeDescription
                         + (significant
                             ? $"  (reason: {sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue} < {sailDiffSettings.Alpha} )"
                             : $"  (reason: {sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue} > {sailDiffSettings.Alpha})");
        stringBuilder.AppendLine(changeLine);
        stringBuilder.AppendLine();

        var tableValues = new List<Table>
        {
            new(
                "Mean",
                Math.Round(sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanBefore, 4),
                Math.Round(sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanAfter, 4)
            ),
            new(
                "Median",
                Math.Round(sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianBefore, 4),
                Math.Round(sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianAfter, 4)
            ),
            new(
                "Sample Size",
                sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeBefore,
                sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeAfter
            )
        };

        stringBuilder.AppendLine(tableValues.ToStringTable(
            new[] { "", "", "" },
            new[] { "", "Before (ms)", "After (ms)" },
            t => t.Name,
            t => t.Before,
            t => t.After));

        return stringBuilder.ToString();
    }

    private static string CreateImpactSummary(SailDiffResult sailDiffResult, SailDiffSettings sailDiffSettings)
    {
        var stats = sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult;
        var percentChange = stats.MeanBefore > 0 ? ((stats.MeanAfter - stats.MeanBefore) / stats.MeanBefore) * 100 : 0;
        var isSignificant = stats.PValue < sailDiffSettings.Alpha &&
                           !stats.ChangeDescription.Contains("No Change", StringComparison.OrdinalIgnoreCase);

        if (!isSignificant)
        {
            return $"⚪ IMPACT: {Math.Abs(percentChange):F1}% difference (NO CHANGE)\n" +
                   $"   P-Value: {stats.PValue:F6} | Mean: {stats.MeanBefore:F3}ms → {stats.MeanAfter:F3}ms";
        }

        var isImprovement = percentChange < 0;
        var direction = isImprovement ? "faster" : "slower";
        var significance = isImprovement ? "IMPROVED" : "REGRESSED";
        var icon = isImprovement ? "🟢" : "🔴";

        return $"{icon} IMPACT: {Math.Abs(percentChange):F1}% {direction} ({significance})\n" +
               $"   P-Value: {stats.PValue:F6} | Mean: {stats.MeanBefore:F3}ms → {stats.MeanAfter:F3}ms";
    }
}