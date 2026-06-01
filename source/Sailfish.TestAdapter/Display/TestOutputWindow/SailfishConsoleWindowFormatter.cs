using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Extensions.Methods;
using Sailfish.Extensions.Types;
using Sailfish.Logging;
using Sailfish.Presentation;

namespace Sailfish.TestAdapter.Display.TestOutputWindow;

internal interface ISailfishConsoleWindowFormatter
{
    string FormConsoleWindowMessageForSailfish(IEnumerable<IClassExecutionSummary> results, OrderedDictionary? tags = null);
}

internal class SailfishConsoleWindowFormatter : ISailfishConsoleWindowFormatter
{
    private readonly ILogger _logger;
    private readonly IRunSettings? _runSettings;

    public SailfishConsoleWindowFormatter(ILogger logger, IRunSettings? runSettings = null)
    {
        _logger = logger;
        _runSettings = runSettings;
    }

    // Decimals to show within the auto-selected time unit (e.g. "1.100 µs").
    private const int DecimalsInUnit = 3;

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
            _logger.Log(LogLevel.Error, exceptionString);
            return exceptionString;
        }

        var consoleOutputString = FormOutputTable(compiledResults);
        _logger.Log(LogLevel.Information, "{MarkdownTable}", consoleOutputString);
        return consoleOutputString;
    }

    private string FormOutputTable(ICompiledTestCaseResult testCaseResult)
    {
        if (testCaseResult.PerformanceRunResult == null || testCaseResult.PerformanceRunResult.SampleSize == 0)
            return string.Empty;
        var testCaseName = testCaseResult.TestCaseId;
        var results = testCaseResult.PerformanceRunResult!;

        var clean = results.DataWithOutliersRemoved;
        var hasClean = clean.Length > 0;
        var cleanMin = hasClean ? clean.Min() : 0d;
        var cleanMax = hasClean ? clean.Max() : 0d;

        // Pick a single, magnitude-appropriate unit (ns/µs/ms/s) for the whole Time column so that
        // fast benchmarks aren't flattened to 0.000ms. Driven by the central values plus the range.
        var magnitudeValues = new List<double> { results.Mean, results.Median };
        if (hasClean)
        {
            magnitudeValues.Add(cleanMin);
            magnitudeValues.Add(cleanMax);
        }

        var unit = DurationFormatter.SelectUnit(magnitudeValues);
        var unitLabel = DurationFormatter.UnitLabel(unit);

        var momentTable = new List<Row>
        {
            new(clean.Length, "N"),
            new(DurationFormatter.Format(results.Mean, unit, DecimalsInUnit), "Mean"),
            new(DurationFormatter.Format(results.Median, unit, DecimalsInUnit), "Median")
        };

        // Add one or more CI rows
        if (results.ConfidenceIntervals != null && results.ConfidenceIntervals.Count > 0)
        {
            foreach (var ci in results.ConfidenceIntervals.OrderBy(x => x.ConfidenceLevel))
            {
                var moeDisplay = DurationFormatter.FormatAdaptive(ci.MarginOfError, unit);
                momentTable.Add(new Row(moeDisplay, $"{ci.ConfidenceLevel:P0} CI ±"));
            }
        }
        else
        {
            // Fallback to legacy single CI
            var moeDisplay = DurationFormatter.FormatAdaptive(results.MarginOfError, unit);
            momentTable.Add(new Row(moeDisplay, $"{results.ConfidenceLevel:P0} CI ±"));
        }

        if (hasClean)
        {
            momentTable.AddRange(new[]
            {
                new Row(DurationFormatter.Format(cleanMin, unit, DecimalsInUnit), "Min"),
                new Row(DurationFormatter.Format(cleanMax, unit, DecimalsInUnit), "Max")
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
            ["Stat", $" Time ({unitLabel})"],
            x => x.Name, x => x.Item));

        // distribution plot (histogram) — sits directly under Descriptive Statistics; the raw outlier
        // and distribution dumps follow below.
        if ((_runSettings?.EnableDistributionPlots ?? true) && hasClean)
        {
            var series = new[]
            {
                new DistributionPlotRenderer.Series(
                    string.Empty,
                    results.DataWithOutliersRemoved,
                    results.Mean,
                    results.Median,
                    results.UpperOutliers.Concat(results.LowerOutliers).ToArray())
            };
            var style = _runSettings?.DistributionPlotStyle ?? DistributionPlotStyle.Histogram;
            var plot = DistributionPlotRenderer.Render(series, unit, style);
            if (!string.IsNullOrEmpty(plot))
            {
                stringBuilder.AppendLine();
                const string textLinePlot = "Distribution Plot";
                stringBuilder.AppendLine(textLinePlot);
                stringBuilder.AppendLine(new string('-', textLinePlot.Length));
                stringBuilder.Append(plot);
            }
        }

        // outliers section
        stringBuilder.AppendLine();
        var textLineOutliers = $"Outliers Removed ({testCaseResult.PerformanceRunResult.TotalNumOutliers})";
        stringBuilder.AppendLine(textLineOutliers);
        stringBuilder.AppendLine(string.Join("", Enumerable.Range(0, textLineOutliers.Length).Select(x => "-")));

        if (testCaseResult.PerformanceRunResult.UpperOutliers.Length > 0)
            stringBuilder.AppendLine($"{testCaseResult.PerformanceRunResult.UpperOutliers.Length} Upper Outliers: " +
                                     string.Join(", ",
                                         testCaseResult.PerformanceRunResult.UpperOutliers
                                             .Select(x => DurationFormatter.Format(x, unit, DecimalsInUnit))));

        if (testCaseResult.PerformanceRunResult.LowerOutliers.Length > 0)
            stringBuilder.AppendLine($"{testCaseResult.PerformanceRunResult.LowerOutliers.Length} Lower Outliers: " +
                                     string.Join(", ",
                                         testCaseResult.PerformanceRunResult.LowerOutliers
                                             .Select(x => DurationFormatter.Format(x, unit, DecimalsInUnit))));

        // distribution (raw cleaned samples)
        var textLineDist = $"Distribution ({unitLabel})";
        stringBuilder.AppendLine();
        stringBuilder.AppendLine(textLineDist);
        stringBuilder.AppendLine(new string('-', textLineDist.Length));
        stringBuilder.AppendLine(string.Join(", ", results.DataWithOutliersRemoved.Select(x => DurationFormatter.Format(x, unit, DecimalsInUnit))));

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
}