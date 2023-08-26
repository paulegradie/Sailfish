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


        var ideOutputContent = summaryResults.Single().CompiledTestCaseResults.Count > 1
            ? CreateFullTable(summaryResults)
            : CreateIdeTestOutputWindowContent(summaryResults.Single().CompiledTestCaseResults.Single());

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

    private static string CreateIdeTestOutputWindowContent(ICompiledTestCaseResult testCaseResult)
    {
        var testCaseName = testCaseResult.TestCaseId;
        var results = testCaseResult.DescriptiveStatisticsResult!;
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(testCaseName?.TestCaseName.Name);
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("-- Moments --");
        stringBuilder.AppendLine("Mean:   " + Math.Round(results.Mean, 4) + " ms");
        stringBuilder.AppendLine("Median: " + Math.Round(results.Median, 4) + " ms");
        stringBuilder.AppendLine("StdDev: " + Math.Round(results.StdDev, 4) + " ms");
        stringBuilder.AppendLine("Num Samples: " + results.NumIterations);
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("-- Raw Results --");
        stringBuilder.AppendLine("Min: " + Math.Round(results.RawExecutionResults.Min(), 4) + " ms");
        stringBuilder.AppendLine("Max: " + Math.Round(results.RawExecutionResults.Max(), 4) + " ms");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("-- Adjusted Raw Results --");
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
        stringBuilder.AppendLine("-- Statistical Test --");
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
            new() { Name = "Median", Before = Math.Round(testCaseResults.TestResults.MedianBefore, 4), After = Math.Round(testCaseResults.TestResults.MedianAfter, 4) }
        };
        var sampleSize = new List<Table>()
        {
            new() { Name = "Sample Size", Before = testCaseResults.TestResults.SampleSizeBefore, After = testCaseResults.TestResults.SampleSizeAfter }
        };

        stringBuilder.AppendLine(tableValues.ToStringTable(
            t => t.Name,
            t => t.Before,
            t => t.After));

        stringBuilder.AppendLine(sampleSize.ToStringTable(t => t.Name, t => t.Before, t => t.After));

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