using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Methods;
using Sailfish.Presentation;

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
        var stats = sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult;
        var significant = stats.PValue < sailDiffSettings.Alpha;
        var changeLine = "Change:          "
                         + stats.ChangeDescription
                         + (significant
                             ? $"  (reason: {stats.PValue} < {sailDiffSettings.Alpha} )"
                             : $"  (reason: {stats.PValue} > {sailDiffSettings.Alpha})");
        stringBuilder.AppendLine(changeLine);

        // Magnitude — shift estimate and standardised effect size. The "by how much"
        // companions to the binary verdict above; nullable so older paths that don't
        // populate them stay rendering the same as before Tier 2.
        if (stats.Difference is { } diff)
            stringBuilder.AppendLine("Shift:           " + FormatDifference(diff));
        if (stats.EffectSize is { } effect)
            stringBuilder.AppendLine("Effect:          " + FormatEffectSize(effect));

        stringBuilder.AppendLine();

        // Recompute from the raw samples (full precision) and pick a magnitude-appropriate unit so
        // fast methods aren't flattened to 0.000ms. Round honors the configured decimal count.
        var display = SailDiffDisplayStatistics.From(stats);
        var unit = DurationFormatter.SelectUnit(new[] { display.MeanBefore, display.MeanAfter, display.MedianBefore, display.MedianAfter });
        var unitLabel = DurationFormatter.UnitLabel(unit);
        var decimals = Math.Max(0, sailDiffSettings.Round);

        var tableValues = new List<Table>
        {
            new("Mean", DurationFormatter.Format(display.MeanBefore, unit, decimals), DurationFormatter.Format(display.MeanAfter, unit, decimals)),
            new("Median", DurationFormatter.Format(display.MedianBefore, unit, decimals), DurationFormatter.Format(display.MedianAfter, unit, decimals)),
            new("Sample Size", stats.SampleSizeBefore.ToString(), stats.SampleSizeAfter.ToString())
        };

        stringBuilder.AppendLine(tableValues.ToStringTable(
            new[] { "", "", "" },
            new[] { "", $"Before ({unitLabel})", $"After ({unitLabel})" },
            t => t.Name,
            t => t.Before,
            t => t.After));

        return stringBuilder.ToString();
    }

    private static string CreateImpactSummary(SailDiffResult sailDiffResult, SailDiffSettings sailDiffSettings)
    {
        var stats = sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult;
        var display = SailDiffDisplayStatistics.From(stats);
        var percentChange = display.MeanBefore > 0 ? ((display.MeanAfter - display.MeanBefore) / display.MeanBefore) * 100 : 0;
        var isSignificant = stats.PValue < sailDiffSettings.Alpha &&
                           !stats.ChangeDescription.Contains("No Change", StringComparison.OrdinalIgnoreCase);

        // Append magnitude details: shift estimate (with CI) and effect size, separated by
        // pipes on the third line so the impact banner stays scannable. Falls back gracefully
        // when the underlying test didn't emit either field.
        var magnitudeLine = BuildMagnitudeLine(stats);

        // Recompute means from the raw samples and auto-select a unit so fast methods aren't 0.000ms.
        var unit = DurationFormatter.SelectUnit(new[] { display.MeanBefore, display.MeanAfter });
        var meanLine = $"   P-Value: {stats.PValue:F6} | Mean: {DurationFormatter.FormatWithUnit(display.MeanBefore, unit, 3)} → {DurationFormatter.FormatWithUnit(display.MeanAfter, unit, 3)}";

        if (!isSignificant)
        {
            return $"⚪ IMPACT: {Math.Abs(percentChange):F1}% difference from baseline (NOT SIGNIFICANT)\n" + meanLine + magnitudeLine;
        }

        var isImprovement = percentChange < 0;
        var direction = isImprovement ? "faster" : "slower";
        var significance = isImprovement ? "IMPROVED" : "REGRESSED";
        var icon = isImprovement ? "🟢" : "🔴";

        // Before = baseline, After = the run under test; name the baseline so "slower/faster" has a referent.
        return $"{icon} IMPACT: {Math.Abs(percentChange):F1}% {direction} than baseline ({significance})\n" + meanLine + magnitudeLine;
    }

    private static string BuildMagnitudeLine(StatisticalTestResult stats)
    {
        var parts = new List<string>();
        if (stats.Difference is { } diff) parts.Add("Shift: " + FormatDifference(diff));
        if (stats.EffectSize is { } effect) parts.Add("Effect: " + FormatEffectSize(effect));
        return parts.Count == 0 ? string.Empty : "\n   " + string.Join(" | ", parts);
    }

    private static string FormatDifference(DifferenceReport diff)
    {
        var ci = diff.CiLower.HasValue && diff.CiUpper.HasValue
            ? $" [{diff.CiLower.Value:F3}, {diff.CiUpper.Value:F3} {diff.Units}]"
            : string.Empty;
        var sign = diff.Value >= 0 ? "+" : string.Empty;
        return $"{diff.Name} = {sign}{diff.Value:F3} {diff.Units}{ci}";
    }

    private static string FormatEffectSize(EffectSizeReport effect)
    {
        var ci = effect.CiLower.HasValue && effect.CiUpper.HasValue
            ? $" [{effect.CiLower.Value:F3}, {effect.CiUpper.Value:F3}]"
            : string.Empty;
        return $"{effect.Name} = {effect.Value:F3}{ci}";
    }
}