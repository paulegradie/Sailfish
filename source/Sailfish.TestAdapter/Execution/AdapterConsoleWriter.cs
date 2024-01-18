using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Extensions.Methods;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sailfish.TestAdapter.Execution;

internal interface IAdapterConsoleWriter : IConsoleWriter
{
    void WriteString(string content, TestMessageLevel testMessageLevel);

    void RecordStart(TestCase testCase);

    void RecordResult(TestResult testResult);

    void RecordEnd(TestCase testCase, TestOutcome testOutcome);

    string WriteTestResultsToIdeConsole(SailDiffResult sailDiffResult, TestIds testIds, SailDiffSettings sailDiffSettings);
}

internal class AdapterConsoleWriter : IAdapterConsoleWriter
{
    private readonly ILogger consoleLogger;
    private readonly IMarkdownTableConverter markdownTableConverter;
    private readonly IFrameworkHandle? messageLogger;

    public AdapterConsoleWriter(
        IMarkdownTableConverter markdownTableConverter,
        IFrameworkHandle? messageLogger)
    {
        this.markdownTableConverter = markdownTableConverter;
        this.messageLogger = messageLogger;
        consoleLogger = new DefaultLogger(LogLevel.Verbose);
    }

    public string WriteToConsole(IEnumerable<IClassExecutionSummary> results, OrderedDictionary? tags = null)
    {
        var summaryResults = results.ToList();
        foreach (var result in summaryResults)
            foreach (var compiledResult in result.CompiledTestCaseResults)
            {
                if (compiledResult.Exception is null) continue;

                WriteMessage(compiledResult.Exception.Message, TestMessageLevel.Error);
                consoleLogger.Log(LogLevel.Error, "{Error}", compiledResult.Exception.Message);

                if (compiledResult.Exception.StackTrace == null) continue;
                WriteMessage(compiledResult.Exception.StackTrace, TestMessageLevel.Error);
                consoleLogger.Log(LogLevel.Error, "{StackTrace}", compiledResult.Exception.StackTrace);

                if (compiledResult.Exception.InnerException is null) continue;
                WriteMessage(compiledResult.Exception.InnerException.Message, TestMessageLevel.Error);
                consoleLogger.Log(LogLevel.Error, "{InnerError}", compiledResult.Exception.InnerException.Message);

                if (compiledResult.Exception.InnerException.StackTrace == null) continue;
                WriteMessage(compiledResult.Exception.InnerException.StackTrace, TestMessageLevel.Error);
                consoleLogger.Log(LogLevel.Error, "{InnerStackTrace}", compiledResult.Exception.InnerException.StackTrace);
            }

        string ideOutputContent;
        if (summaryResults.Count > 1 || summaryResults.Single().CompiledTestCaseResults.Count() > 1)
            ideOutputContent = CreateFullTable(summaryResults);
        else
            ideOutputContent = CreateIdeTestOutputWindowContent(summaryResults.Single().CompiledTestCaseResults.Single());

        WriteMessage(ideOutputContent, TestMessageLevel.Informational);
        consoleLogger.Log(LogLevel.Information, "{MarkdownTable}", ideOutputContent);

        return ideOutputContent;
    }

    public void WriteStatTestResultsToConsole(string markdownBody, TestIds testIds, SailDiffSettings sailDiffSettings)
    {
        var stringBuilder = new StringBuilder();
        BuildHeader(stringBuilder, testIds.BeforeTestIds, testIds.AfterTestIds, sailDiffSettings);
        stringBuilder.AppendLine(markdownBody);
        var result = stringBuilder.ToString();
        consoleLogger.Log(LogLevel.Information, result);
        WriteMessage(result, TestMessageLevel.Informational);
    }

    public string WriteTestResultsToIdeConsole(SailDiffResult sailDiffResult, TestIds testIds, SailDiffSettings sailDiffSettings)
    {
        var stringBuilder = new StringBuilder();
        return StatisticalTestFailsThenBailAndReturnNothing(sailDiffResult, stringBuilder, out var nothingToWrite)
            ? nothingToWrite
            : FormattedSailDiffResult(sailDiffResult, sailDiffSettings, stringBuilder);
    }

    public void WriteString(string content)
    {
        WriteString(content, TestMessageLevel.Informational);
    }

    public void WriteString(string content, TestMessageLevel messageLevel)
    {
        consoleLogger.Log(LogLevel.Information, content);
        WriteMessage(content, messageLevel);
    }

    public void RecordStart(TestCase testCase)
    {
        messageLogger?.RecordStart(testCase);
    }

    public void RecordResult(TestResult testResult)
    {
        messageLogger?.RecordResult(testResult);
    }

    public void RecordEnd(TestCase testCase, TestOutcome testOutcome)
    {
        messageLogger?.RecordEnd(testCase, testOutcome);
    }

    private string CreateFullTable(IReadOnlyCollection<IClassExecutionSummary> summaryResults)
    {
        var rawData = summaryResults
            .SelectMany(x =>
                x.CompiledTestCaseResults.SelectMany(y =>
                    y.PerformanceRunResult?.RawExecutionResults ?? Array.Empty<double>()))
            .ToArray();

        return markdownTableConverter.ConvertToMarkdownTableString(summaryResults)
               + "Raw results: \n"
               + string.Join(", ", rawData);
    }

    private static string CreateIdeTestOutputWindowContent(ICompiledTestCaseResult testCaseResult)
    {
        if (testCaseResult.Exception is not null)
        {
            var exceptionBuilder = new StringBuilder();
            exceptionBuilder.AppendLine("____ Exceptions ____");
            exceptionBuilder.AppendLine(string.Join("\n\n", testCaseResult.Exception.Message));
            return exceptionBuilder.ToString();
        }

        if (testCaseResult.PerformanceRunResult == null || testCaseResult.PerformanceRunResult.SampleSize == 0) return string.Empty;
        var testCaseName = testCaseResult.TestCaseId;
        var results = testCaseResult.PerformanceRunResult!;

        var momentTable = new List<Row>
        {
            new(Math.Round(results.Mean, 4), "Mean"),
            new(Math.Round(results.Median, 4), "Median"),
            new(Math.Round(results.StdDev, 4), "StdDev"),
            new(Math.Round(results.RawExecutionResults.Min(), 4), "Min"),
            new(Math.Round(results.RawExecutionResults.Max(), 4), "Max")
        };

        var stringBuilder = new StringBuilder();

        // main moments
        stringBuilder.AppendLine(testCaseName?.TestCaseName.Name);
        stringBuilder.AppendLine();
        const string textLineStats = "Descriptive Statistics";
        stringBuilder.AppendLine(textLineStats);
        stringBuilder.AppendLine(string.Join("", Enumerable.Range(0, textLineStats.Length).Select(x => "-")));
        stringBuilder.AppendLine(momentTable.ToStringTable(
            new[] { "", "" },
            new[] { "Stat", " Time (ms)" },
            x => x.Name, x => x.Item));

        // outliers section
        stringBuilder.AppendLine();
        var textLineOutliers = $"Outliers Removed ({testCaseResult.PerformanceRunResult.TotalNumOutliers})";
        stringBuilder.AppendLine(textLineOutliers);
        stringBuilder.AppendLine(string.Join("", Enumerable.Range(0, textLineOutliers.Length).Select(x => "-")));

        if (testCaseResult.PerformanceRunResult.UpperOutliers.Length > 0)
            stringBuilder.AppendLine($"{testCaseResult.PerformanceRunResult.UpperOutliers.Length} Upper Outliers: " +
                                     string.Join(", ", testCaseResult.PerformanceRunResult.UpperOutliers.Select(x => Math.Round(x, 4))));

        if (testCaseResult.PerformanceRunResult.LowerOutliers.Length > 0)
            stringBuilder.AppendLine($"{testCaseResult.PerformanceRunResult.LowerOutliers.Length} Lower Outliers: " +
                                     string.Join(", ", testCaseResult.PerformanceRunResult.LowerOutliers.Select(x => Math.Round(x, 4))));

        // distribution
        const string textLineDist = "Distribution (ms)";
        stringBuilder.AppendLine();
        stringBuilder.AppendLine(textLineDist);
        stringBuilder.AppendLine(string.Join("", Enumerable.Range(0, textLineDist.Length).Select(x => "-")));
        stringBuilder.AppendLine(string.Join(", ", results.DataWithOutliersRemoved.Select(x => Math.Round(x, 4))));

        return stringBuilder.ToString();
    }

    private static string FormattedSailDiffResult(SailDiffResult sailDiffResult, SailDiffSettings sailDiffSettings, StringBuilder stringBuilder)
    {
        const string testLine = "Statistical Test";
        stringBuilder.AppendLine(testLine);
        stringBuilder.AppendLine(string.Join("", Enumerable.Range(0, testLine.Length).Select(x => "-")));

        stringBuilder.AppendLine("Test Used:       " + sailDiffSettings.TestType);
        stringBuilder.AppendLine("PVal Threshold:  " + sailDiffSettings.Alpha);
        stringBuilder.AppendLine("PValue:          " + sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue);
        var significant = sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue < sailDiffSettings.Alpha;
        var changeLine = "Change:          "
                         + sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.ChangeDescription
                         + (significant
                             ? $"  (reason: {sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue} < {sailDiffSettings.Alpha} )"
                             : $"  (reason: {sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.PValue} > {sailDiffSettings.Alpha})");
        stringBuilder.AppendLine(changeLine);
        stringBuilder.AppendLine();

        var tableValues = new List<Table>
        {
            new()
            {
                Name = "Mean", Before = Math.Round(sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanBefore, 4),
                After = Math.Round(sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.MeanAfter, 4)
            },
            new()
            {
                Name = "Median", Before = Math.Round(sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianBefore, 4),
                After = Math.Round(sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.MedianAfter, 4)
            },
            new()
            {
                Name = "Sample Size", Before = sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeBefore,
                After = sailDiffResult.TestResultsWithOutlierAnalysis.StatisticalTestResult.SampleSizeAfter
            }
        };

        stringBuilder.AppendLine(tableValues.ToStringTable(
            new[] { "", "", "" },
            new[] { "", "Before (ms)", "After (ms)" },
            t => t.Name,
            t => t.Before,
            t => t.After));

        return stringBuilder.ToString();
    }

    private static bool StatisticalTestFailsThenBailAndReturnNothing(SailDiffResult sailDiffResult, StringBuilder stringBuilder, out string s)
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

    private void WriteMessage(string message, TestMessageLevel messageLevel)
    {
        if (!string.IsNullOrEmpty(message)) messageLogger?.SendMessage(messageLevel, message);
    }

    private static void BuildHeader(
        StringBuilder stringBuilder,
        IEnumerable<string> beforeIds,
        IEnumerable<string> afterIds,
        SailDiffSettings sailDiffSettings)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("-----------------------------------");
        stringBuilder.AppendLine($"{sailDiffSettings.TestType} results comparing:");
        stringBuilder.AppendLine($"Before: {string.Join(", ", beforeIds)}");
        stringBuilder.AppendLine($"After: {string.Join(", ", afterIds)}");
        stringBuilder.AppendLine("-----------------------------------\r");
        stringBuilder.AppendLine($"Note: Changes are significant if the PValue is less than {sailDiffSettings.Alpha}");
    }

    private class Row
    {
        public Row(double item, string name)
        {
            Name = name;
            Item = item;
        }

        public string Name { get; }
        public double Item { get; }
    }

    private class Table
    {
        public string Name { get; init; } = null!;
        public double Before { get; init; }
        public double After { get; init; }
    }
}