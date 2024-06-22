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
        stringBuilder.AppendLine($"Before Ids: {string.Join(", ", testIds.BeforeTestIds)}");
        stringBuilder.AppendLine($"After Ids: {string.Join(", ", testIds.AfterTestIds)}");

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
}