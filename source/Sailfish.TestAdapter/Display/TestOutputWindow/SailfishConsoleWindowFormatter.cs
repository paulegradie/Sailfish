using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Extensions.Methods;
using Sailfish.Extensions.Types;
using Sailfish.Logging;

namespace Sailfish.TestAdapter.Display.TestOutputWindow;

internal interface ISailfishConsoleWindowFormatter
{
    string FormConsoleWindowMessageForSailfish(IEnumerable<IClassExecutionSummary> results, OrderedDictionary? tags = null);
}

internal class SailfishConsoleWindowFormatter : ISailfishConsoleWindowFormatter
{
    private readonly ILogger logger;

    public SailfishConsoleWindowFormatter(ILogger logger)
    {
        this.logger = logger;
    }

    public string FormConsoleWindowMessageForSailfish(IEnumerable<IClassExecutionSummary> results, OrderedDictionary? tags = null)
    {
        var summaryResults = results.ToList();
        var compiledResults = summaryResults.SingleOrDefault()?.CompiledTestCaseResults.SingleOrDefault();

        if (compiledResults is null) return "No results to report";
        if (compiledResults.Exception is not null)
        {
            var exceptionBuilder = new StringBuilder();
            exceptionBuilder.AppendLine("____ Exception Encountered ____");
            if (compiledResults.Exception.InnerException is not null)
            {
                exceptionBuilder.AppendLine("Inner Stack Trace");
                exceptionBuilder.AppendLine(compiledResults.Exception.InnerException.Message);
                exceptionBuilder.AppendLine(compiledResults.Exception.InnerException.StackTrace);
            }

            exceptionBuilder.AppendLine("StackTrace:");
            exceptionBuilder.AppendLine(compiledResults.Exception.Message);
            exceptionBuilder.AppendLine(compiledResults.Exception.StackTrace);

            var exceptionString = exceptionBuilder.ToString();
            logger.Log(LogLevel.Error, exceptionString);
            return exceptionString;
        }

        var consoleOutputString = FormOutputTable(compiledResults);
        logger.Log(LogLevel.Information, "{MarkdownTable}", consoleOutputString);
        return consoleOutputString;
    }

    private static string FormOutputTable(ICompiledTestCaseResult testCaseResult)
    {
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
}