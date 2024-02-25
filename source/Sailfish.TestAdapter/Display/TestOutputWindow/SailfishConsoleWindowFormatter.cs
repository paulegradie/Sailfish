using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Extensions.Methods;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sailfish.TestAdapter.Display.TestOutputWindow;

internal interface ISailfishConsoleWindowFormatter
{
    string FormConsoleWindowMessageForSailfish(IEnumerable<IClassExecutionSummary> results, OrderedDictionary? tags = null);
}

internal class SailfishConsoleWindowFormatter : ISailfishConsoleWindowFormatter
{
    private readonly IMarkdownTableConverter markdownTableConverter;
    private readonly ILogger logger;

    public SailfishConsoleWindowFormatter(IMarkdownTableConverter markdownTableConverter, ILogger logger)
    {
        this.markdownTableConverter = markdownTableConverter;
        this.logger = logger;
    }

    public string FormConsoleWindowMessageForSailfish(IEnumerable<IClassExecutionSummary> results, OrderedDictionary? tags = null)
    {
        var summaryResults = results.ToList();
        foreach (var result in summaryResults)
        foreach (var compiledResult in result.CompiledTestCaseResults)
        {
            if (compiledResult.Exception is null) continue;

            logger.Log(LogLevel.Error, "{Error}", compiledResult.Exception.Message);

            if (compiledResult.Exception.StackTrace == null) continue;
            logger.Log(LogLevel.Error, "{StackTrace}", compiledResult.Exception.StackTrace);

            if (compiledResult.Exception.InnerException is null) continue;
            logger.Log(LogLevel.Error, "{InnerError}", compiledResult.Exception.InnerException.Message);

            if (compiledResult.Exception.InnerException.StackTrace == null) continue;
            logger.Log(LogLevel.Error, "{InnerStackTrace}", compiledResult.Exception.InnerException.StackTrace);
        }

        string ideOutputContent;
        if (summaryResults.Count > 1 || summaryResults.Single().CompiledTestCaseResults.Count() > 1)
            ideOutputContent = CreateFullTable(summaryResults);
        else
        {
            var compiledResults = summaryResults.SingleOrDefault()?.CompiledTestCaseResults.SingleOrDefault();
            if (compiledResults != null)
            {
                ideOutputContent = CreateIdeTestOutputWindowContent(compiledResults);
            }
            else
            {
                ideOutputContent = string.Empty;
            }
        }

        logger.Log(LogLevel.Information, "{MarkdownTable}", ideOutputContent);

        return ideOutputContent;
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

        if (testCaseResult.PerformanceRunResult == null || testCaseResult.PerformanceRunResult.SampleSize == 0)
            return string.Empty;
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
                                     string.Join(", ",
                                         testCaseResult.PerformanceRunResult.UpperOutliers
                                             .Select(x => Math.Round(x, 4))));

        if (testCaseResult.PerformanceRunResult.LowerOutliers.Length > 0)
            stringBuilder.AppendLine($"{testCaseResult.PerformanceRunResult.LowerOutliers.Length} Lower Outliers: " +
                                     string.Join(", ",
                                         testCaseResult.PerformanceRunResult.LowerOutliers
                                             .Select(x => Math.Round(x, 4))));

        // distribution
        const string textLineDist = "Distribution (ms)";
        stringBuilder.AppendLine();
        stringBuilder.AppendLine(textLineDist);
        stringBuilder.AppendLine(string.Join("", Enumerable.Range(0, textLineDist.Length).Select(x => "-")));
        stringBuilder.AppendLine(string.Join(", ", results.DataWithOutliersRemoved.Select(x => Math.Round(x, 4))));

        return stringBuilder.ToString();
    }
    
    
    private string CreateFullTable(IReadOnlyCollection<IClassExecutionSummary> summaryResults)
    {
        var rawData = summaryResults
            .SelectMany(x =>
                x.CompiledTestCaseResults.SelectMany(y =>
                    y.PerformanceRunResult?.RawExecutionResults ?? []))
            .ToArray();

        return markdownTableConverter.ConvertToMarkdownTableString(summaryResults)
               + "Raw results: \n"
               + string.Join(", ", rawData);
    }

}