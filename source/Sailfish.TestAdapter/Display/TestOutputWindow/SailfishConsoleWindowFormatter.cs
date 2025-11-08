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

        var clean = results.DataWithOutliersRemoved;

        var momentTable = new List<Row>
        {
            new(clean.Length, "N"),
            new(Math.Round(results.Mean, 4), "Mean"),
            new(Math.Round(results.Median, 4), "Median")
        };

        // Add one or more CI rows
        if (results.ConfidenceIntervals != null && results.ConfidenceIntervals.Count > 0)
        {
            foreach (var ci in results.ConfidenceIntervals.OrderBy(x => x.ConfidenceLevel))
            {
                var moeDisplay = FormatAdaptive(ci.MarginOfError);
                momentTable.Add(new Row(moeDisplay, $"{ci.ConfidenceLevel:P0} CI ±"));
            }
        }
        else
        {
            // Fallback to legacy single CI
            var moeDisplay = FormatAdaptive(results.MarginOfError);
            momentTable.Add(new Row(moeDisplay, $"{results.ConfidenceLevel:P0} CI ±"));
        }

        if (clean.Length > 0)
        {
            momentTable.AddRange(new[]
            {
                new Row(Math.Round(clean.Min(), 4), "Min"),
                new Row(Math.Round(clean.Max(), 4), "Max")
            });
        }
        else
        {
            momentTable.AddRange(new[]
            {
                new Row("N/A", "Min"),
                new Row("N/A", "Max")
            });
        }

        var stringBuilder = new StringBuilder();

        // main moments
        stringBuilder.AppendLine(testCaseName?.TestCaseName.Name);
        stringBuilder.AppendLine();
        const string textLineStats = "Descriptive Statistics";
        stringBuilder.AppendLine(textLineStats);
        stringBuilder.AppendLine(string.Join("", Enumerable.Range(0, textLineStats.Length).Select(x => "-")));
        stringBuilder.AppendLine(momentTable.ToStringTable(
            ["", ""],
            ["Stat", " Time (ms)"],
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
        stringBuilder.AppendLine(new string('-', textLineDist.Length));
        stringBuilder.AppendLine(string.Join(", ", results.DataWithOutliersRemoved.Select(x => Math.Round(x, 4))));

        // validation warnings (if any)
        if (results.Validation?.HasWarnings == true)
        {
            stringBuilder.AppendLine();
            const string warningsHeader = "Warnings";
            stringBuilder.AppendLine(warningsHeader);
            stringBuilder.AppendLine(new string('-', warningsHeader.Length));
            foreach (var w in results.Validation!.Warnings)
            {
                var tag = w.Severity switch
                {
                    ValidationSeverity.Critical => "❗",
                    ValidationSeverity.Warning => "⚠️",
                    _ => "ℹ️"
                };
                stringBuilder.AppendLine($"{tag} {w.Message}");
            }
        }

        return stringBuilder.ToString();
    }

        private static string FormatAdaptive(double value)
        {
            if (value == 0) return "0";
            var s4 = value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
            if (!s4.Equals("0.0000")) return s4;
            var s6 = value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
            if (!s6.Equals("0.000000")) return s6;
            var s8 = value.ToString("F8", System.Globalization.CultureInfo.InvariantCulture);
            if (!s8.Equals("0.00000000")) return s8;
            return "0";
        }

}