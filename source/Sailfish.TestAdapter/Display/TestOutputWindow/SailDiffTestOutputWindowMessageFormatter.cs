using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        stringBuilder.AppendLine($"Before Ids: {string.Join(", ", testIds.BeforeTestIds)}");
        stringBuilder.AppendLine($"After Ids: {string.Join(", ", testIds.AfterTestIds)}");
        return StatisticalTestFailsThenBailAndReturnNothing(sailDiffResult, stringBuilder, out var nothingToWrite)
            ? nothingToWrite
            : FormattedSailDiffResult(sailDiffResult, sailDiffSettings, stringBuilder);
    }

    private static bool StatisticalTestFailsThenBailAndReturnNothing(
        SailDiffResult sailDiffResult,
        StringBuilder stringBuilder,
        out string s)
    {
        s = string.Empty;
        if (!sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.Failed) return false;
        stringBuilder.AppendLine("Statistical testing failed:");
        stringBuilder.AppendLine(sailDiffResult.TestResultsWithOutlierAnalysis.ExceptionMessage);
        stringBuilder.AppendLine(sailDiffResult.TestResultsWithOutlierAnalysis.ExceptionMessage);
        {
            s = stringBuilder.ToString();
            return true;
        }
    }

    private static string FormattedSailDiffResult(SailDiffResult sailDiffResult, SailDiffSettings sailDiffSettings,
        StringBuilder stringBuilder)
    {
        const string testLine = "Statistical Test";
        stringBuilder.AppendLine(testLine);
        stringBuilder.AppendLine(string.Join("", Enumerable.Range(0, testLine.Length).Select(x => "-")));

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
                Name: "Mean",
                Before: Math.Round(sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanBefore, 4),
                After: Math.Round(sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanAfter, 4)
            ),
            new(
                Name: "Median",
                Before: Math.Round(sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianBefore, 4),
                After: Math.Round(sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianAfter, 4)
            ),
            new(
                Name: "Sample Size",
                Before: sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeBefore,
                After: sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeAfter
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
}