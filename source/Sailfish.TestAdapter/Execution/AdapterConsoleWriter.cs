using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Analysis;
using Sailfish.Analysis.Saildiff;
using Sailfish.Contracts.Public;
using Sailfish.Execution;
using Sailfish.Extensions.Methods;
using Sailfish.Extensions.Types;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using Sailfish.Statistics;
using Serilog;
using Serilog.Core;


namespace Sailfish.TestAdapter.Execution;

internal interface IAdapterConsoleWriter : IConsoleWriter
{
    void WriteString(string content, TestMessageLevel testMessageLevel);
    void RecordStart(TestCase testCase);
    void RecordResult(TestResult testResult);
    void RecordEnd(TestCase testCase, TestOutcome testOutcome);
    string WriteTestResultsToIdeConsole(TestCaseResults testCaseResults, TestIds testIds, TestSettings testSettings);
}

internal class AdapterConsoleWriter : IAdapterConsoleWriter
{
    private readonly IMarkdownTableConverter markdownTableConverter;
    private readonly IFrameworkHandle? messageLogger;

    private readonly Logger consoleLogger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateLogger();

    public AdapterConsoleWriter(
        IMarkdownTableConverter markdownTableConverter,
        IFrameworkHandle? messageLogger)
    {
        this.markdownTableConverter = markdownTableConverter;
        this.messageLogger = messageLogger;
    }

    public string Present(IEnumerable<IExecutionSummary> results, OrderedDictionary? tags = null)
    {
        var summaryResults = results.ToList();
        foreach (var result in summaryResults)
        {
            foreach (var compiledResult in result.CompiledTestCaseResults)
            {
                if (!compiledResult.Exceptions.Any()) continue;
                foreach (var exception in compiledResult.Exceptions)
                {
                    messageLogger?.SendMessage(TestMessageLevel.Error, exception.Message);
                    consoleLogger.Error("{Error}", exception.Message);

                    if (exception.StackTrace == null) continue;
                    messageLogger?.SendMessage(TestMessageLevel.Error, exception.StackTrace);
                    consoleLogger.Error("{StackTrace}", exception.Message);

                    if (exception.InnerException is null) continue;
                    messageLogger?.SendMessage(TestMessageLevel.Error, exception.InnerException.Message);
                    consoleLogger.Error("{InnerError}", exception.InnerException.Message);

                    if (exception.InnerException.StackTrace == null) continue;
                    messageLogger?.SendMessage(TestMessageLevel.Error, exception.InnerException.StackTrace);
                    consoleLogger.Error("{InnerStackTrace}", exception.InnerException.StackTrace);
                }
            }
        }

        string ideOutputContent;
        if (summaryResults.Count > 1 || summaryResults.Single().CompiledTestCaseResults.Count > 1)
        {
            ideOutputContent = CreateFullTable(summaryResults);
        }
        else
        {
            ideOutputContent = CreateIdeTestOutputWindowContent(summaryResults.Single().CompiledTestCaseResults.Single());
        }

        messageLogger?.SendMessage(TestMessageLevel.Informational, ideOutputContent);
        consoleLogger.Information("{MarkdownTable}", ideOutputContent);

        return ideOutputContent;
    }

    private string CreateFullTable(List<IExecutionSummary> summaryResults)
    {
        var rawData = summaryResults
            .SelectMany(x =>
                x.CompiledTestCaseResults.SelectMany(y =>
                    y.DescriptiveStatisticsResult?.RawExecutionResults ?? Array.Empty<double>()))
            .ToArray();

        var markdownStringTable = markdownTableConverter.ConvertToMarkdownTableString(summaryResults)
                                  + "Raw results: \n"
                                  + string.Join(", ", rawData);
        return markdownStringTable;
    }

    class Row
    {
        public Row(double item, string name)
        {
            Name = name;
            Item = item;
        }

        public string Name { get; set; }
        public double Item { get; set; }
    }

    private static string CreateIdeTestOutputWindowContent(ICompiledTestCaseResult testCaseResult)
    {
        if (testCaseResult.DescriptiveStatisticsResult == null || testCaseResult.DescriptiveStatisticsResult.NumIterations == 0) return string.Empty;
        var testCaseName = testCaseResult.TestCaseId;
        var results = testCaseResult.DescriptiveStatisticsResult!;

        var momentTable = new List<Row>()
        {
            new Row(Math.Round(results.Mean, 4), "Mean"),
            new Row(Math.Round(results.Median, 4), "Median"),
            new Row(Math.Round(results.StdDev, 4), "StdDev"),
            new(Math.Round(results.RawExecutionResults.Min(), 4), "Min"),
            new(Math.Round(results.RawExecutionResults.Max(), 4), "Max")
        };

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(testCaseName?.TestCaseName.Name);
        stringBuilder.AppendLine();
        const string textLineStats = "Descriptive Statistics";
        stringBuilder.AppendLine(textLineStats);
        stringBuilder.AppendLine(string.Join("", Enumerable.Range(0, textLineStats.Length).Select(x => "-")));
        stringBuilder.AppendLine(momentTable.ToStringTable(
            new[] { "", "" },
            new[] { "Stat", " Time (ms)" },
            x => x.Name, x => x.Item));

        const string textLineDist = "Adjusted Distribution (ms)";
        stringBuilder.AppendLine(textLineDist);
        stringBuilder.AppendLine(string.Join("", Enumerable.Range(0, textLineDist.Length).Select(x => "-")));
        stringBuilder.AppendLine(string.Join(", ", results.RawExecutionResults.Select(x => Math.Round(x, 4))));


        return stringBuilder.ToString();
    }

    public void WriteStatTestResultsToConsole(string markdownBody, TestIds testIds, TestSettings testSettings)
    {
        var stringBuilder = new StringBuilder();
        BuildHeader(stringBuilder, testIds.BeforeTestIds, testIds.AfterTestIds, testSettings);
        stringBuilder.AppendLine(markdownBody);
        var result = stringBuilder.ToString();
        consoleLogger.Information(result);
        messageLogger?.SendMessage(TestMessageLevel.Informational, result);
    }

    public string WriteTestResultsToIdeConsole(TestCaseResults testCaseResults, TestIds testIds, TestSettings testSettings)
    {
        var stringBuilder = new StringBuilder();
        const string testLine = "Statistical Test";
        stringBuilder.AppendLine(testLine);
        stringBuilder.AppendLine(string.Join("", Enumerable.Range(0, testLine.Length).Select(x => "-")));

        stringBuilder.AppendLine("Test Used:       " + testSettings.TestType);
        stringBuilder.AppendLine("PVal Threshold:  " + testSettings.Alpha);
        stringBuilder.AppendLine("PValue:          " + testCaseResults.TestResults.PValue);
        var significant = testCaseResults.TestResults.PValue < testSettings.Alpha;
        var changeLine = "Change:          "
                         + testCaseResults.TestResults.ChangeDescription
                         + (significant
                             ? $"  (reason: {testCaseResults.TestResults.PValue} < {testSettings.Alpha} )"
                             : $"  (reason: {testCaseResults.TestResults.PValue} > {testSettings.Alpha})");
        stringBuilder.AppendLine(changeLine);
        stringBuilder.AppendLine();

        var tableValues = new List<Table>()
        {
            new() { Name = "Mean", Before = Math.Round(testCaseResults.TestResults.MeanBefore, 4), After = Math.Round(testCaseResults.TestResults.MeanAfter, 4) },
            new() { Name = "Median", Before = Math.Round(testCaseResults.TestResults.MedianBefore, 4), After = Math.Round(testCaseResults.TestResults.MedianAfter, 4) },
            new() { Name = "Sample Size", Before = testCaseResults.TestResults.SampleSizeBefore, After = testCaseResults.TestResults.SampleSizeAfter }
        };

        stringBuilder.AppendLine(tableValues.ToStringTable(
            new[] { "", "", "" },
            new[] { "", "Before (ms)", "After (ms)" },
            t => t.Name,
            t => t.Before,
            t => t.After));

        return stringBuilder.ToString();
    }

    private class Table
    {
        public string Name { get; set; }
        public double Before { get; set; }
        public double After { get; set; }
    }

    public void WriteString(string content)
    {
        WriteString(content, TestMessageLevel.Informational);
    }

    public void WriteString(string content, TestMessageLevel messageLevel)
    {
        consoleLogger.Information(content);
        messageLogger?.SendMessage(messageLevel, content);
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

    private static void BuildHeader(StringBuilder stringBuilder, IEnumerable<string> beforeIds, IEnumerable<string> afterIds, TestSettings testSettings)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("-----------------------------------");
        stringBuilder.AppendLine($"{testSettings.TestType} results comparing:");
        stringBuilder.AppendLine($"Before: {string.Join(", ", beforeIds)}");
        stringBuilder.AppendLine($"After: {string.Join(", ", afterIds)}");
        stringBuilder.AppendLine("-----------------------------------\r");
        stringBuilder.AppendLine($"Note: Changes are significant if the PValue is less than {testSettings.Alpha}");
    }
}