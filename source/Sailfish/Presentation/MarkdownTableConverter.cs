using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Extensions.Methods;

namespace Sailfish.Presentation;

public interface IMarkdownTableConverter
{
    string ConvertToMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries);

    string ConvertToMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries, Func<IClassExecutionSummary, bool> summaryFilter);

    string ConvertToEnhancedMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries);

    string ConvertToEnhancedMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries, Func<IClassExecutionSummary, bool> summaryFilter);

    string ConvertScaleFishResultToMarkdown(IEnumerable<ScalefishClassModel> testClassComplexityResults);
}

public class MarkdownTableConverter : IMarkdownTableConverter
{
    private readonly ISailDiffUnifiedFormatter? _unifiedFormatter;

    public MarkdownTableConverter()
    {
        // Default constructor for backward compatibility
        _unifiedFormatter = null;
    }

    public MarkdownTableConverter(ISailDiffUnifiedFormatter unifiedFormatter)
    {
        _unifiedFormatter = unifiedFormatter ?? throw new ArgumentNullException(nameof(unifiedFormatter));
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

    public string ConvertToMarkdownTableString(
        IEnumerable<IClassExecutionSummary> executionSummaries,
        Func<IClassExecutionSummary, bool> summaryFilter)
    {
        var filteredSummaries = executionSummaries.Where(summaryFilter);
        return ConvertToMarkdownTableString(filteredSummaries);
    }

    public string ConvertToEnhancedMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries)
    {
        return ConvertToEnhancedMarkdownTableString(executionSummaries, _ => true);
    }

    public string ConvertToEnhancedMarkdownTableString(
        IEnumerable<IClassExecutionSummary> executionSummaries,
        Func<IClassExecutionSummary, bool> summaryFilter)
    {
        var filteredSummaries = executionSummaries.Where(summaryFilter);
        return CreateEnhancedMarkdownOutput(filteredSummaries);
    }

    public string ConvertToMarkdownTableString(IEnumerable<IClassExecutionSummary> executionSummaries)
    {
        var stringBuilder = new StringBuilder();

        var allExceptions = new List<Exception>();
        foreach (var result in executionSummaries)
        {
            AppendResults(result.TestClass.Name, result.CompiledTestCaseResults, stringBuilder);
            allExceptions.AddRange(result.CompiledTestCaseResults.Where(x => x.Exception is not null).Select(x => x.Exception).Cast<Exception>().ToList());
        }

        AppendExceptions(allExceptions, stringBuilder);

        return stringBuilder.ToString();
    }

    private string CreateEnhancedMarkdownOutput(IEnumerable<IClassExecutionSummary> executionSummaries)
    {
        var stringBuilder = new StringBuilder();

        // Add document header
        stringBuilder.AppendLine("# 📊 Performance Test Results");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"**Generated:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        stringBuilder.AppendLine();

        var allExceptions = new List<Exception>();
        var hasResults = false;

        foreach (var result in executionSummaries)
        {
            if (result.CompiledTestCaseResults.Any())
            {
                hasResults = true;
                AppendEnhancedResults(result.TestClass.Name, result.CompiledTestCaseResults, stringBuilder);
                allExceptions.AddRange(result.CompiledTestCaseResults.Where(x => x.Exception is not null).Select(x => x.Exception).Cast<Exception>().ToList());
            }
        }

        if (!hasResults)
        {
            stringBuilder.AppendLine("No performance test results available.");
        }

        AppendExceptions(allExceptions, stringBuilder);

        return stringBuilder.ToString();
    }

    private static void AppendEnhancedResults(string typeName, IEnumerable<ICompiledTestCaseResult> compiledResults, StringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"## 🧪 {typeName}");
        stringBuilder.AppendLine();

        foreach (var group in compiledResults.GroupBy(x => x.GroupingId))
        {
            if (group.Key is null) continue;

            var groupResults = group.ToList();
            var n = groupResults.Select(x => x.PerformanceRunResult?.SampleSize).Distinct().Single();
            if (n is null or 0) continue;

            // Add group header if there are multiple groups
            if (groupResults.Count > 1 || !string.IsNullOrEmpty(group.Key))
            {
                stringBuilder.AppendLine($"### 📈 {group.Key}");
                stringBuilder.AppendLine();
            }

            // Add performance summary if unified formatter is available
            var performanceSummary = CreatePerformanceSummary(groupResults);
            if (!string.IsNullOrEmpty(performanceSummary))
            {
                stringBuilder.AppendLine(performanceSummary);
                stringBuilder.AppendLine();
            }

            // Add detailed results table
            var table = groupResults.ToStringTable(
                typeName,
                new List<string>
                {
                    "",
                    "ms",
                    "ms",
                    "ms",
                    ""
                },
                new List<string>
                {
                    "Display Name",
                    "Mean",
                    "Median",
                    $"StdDev (N={n})",
                    "Variance"
                },
                u => u.TestCaseId!.DisplayName,
                u => u.PerformanceRunResult!.Mean,
                u => u.PerformanceRunResult!.Median,
                u => u.PerformanceRunResult!.StdDev,
                u => u.PerformanceRunResult!.Variance
            );

            stringBuilder.AppendLine(table);
            stringBuilder.AppendLine();
            // Add CI summary lines for each test in the group (95% and 99% if available)
            foreach (var r in groupResults.Where(gr => gr.PerformanceRunResult != null))
            {
                var pr = r.PerformanceRunResult!;
                if (pr.ConfidenceIntervals != null && pr.ConfidenceIntervals.Count > 0)
                {
                    var ciParts = pr.ConfidenceIntervals
                        .OrderBy(ci => ci.ConfidenceLevel)
                        .Select(ci => $"{ci.ConfidenceLevel:P0} CI ± {FormatAdaptive(ci.MarginOfError)}ms");
                    stringBuilder.AppendLine($"- {r.TestCaseId!.DisplayName}: {string.Join(", ", ciParts)}");
                }
                else
                {
                    stringBuilder.AppendLine($"- {r.TestCaseId!.DisplayName}: {pr.ConfidenceLevel:P0} CI ± {FormatAdaptive(pr.MarginOfError)}ms");
                }
            }
            stringBuilder.AppendLine();

        }
    }

    private static string CreatePerformanceSummary(List<ICompiledTestCaseResult> groupResults)
    {
        if (groupResults.Count < 2) return string.Empty;

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("**📊 Performance Summary:**");
        stringBuilder.AppendLine();

        // Find fastest and slowest methods
        var validResults = groupResults.Where(r => r.PerformanceRunResult != null).ToList();
        if (validResults.Count < 2) return string.Empty;

        var fastest = validResults.OrderBy(r => r.PerformanceRunResult!.Mean).First();
        var slowest = validResults.OrderByDescending(r => r.PerformanceRunResult!.Mean).First();

        if (fastest.TestCaseId?.DisplayName != slowest.TestCaseId?.DisplayName)
        {
            var speedDifference = (slowest.PerformanceRunResult!.Mean - fastest.PerformanceRunResult!.Mean) / fastest.PerformanceRunResult!.Mean * 100;

            stringBuilder.AppendLine($"- 🟢 **Fastest:** {fastest.TestCaseId?.DisplayName} ({fastest.PerformanceRunResult!.Mean:F3}ms)");
            stringBuilder.AppendLine($"- 🔴 **Slowest:** {slowest.TestCaseId?.DisplayName} ({slowest.PerformanceRunResult!.Mean:F3}ms)");
            stringBuilder.AppendLine($"- 📈 **Performance Gap:** {speedDifference:F1}% difference");
        }

        return stringBuilder.ToString();
    }

    public string ConvertScaleFishResultToMarkdown(IEnumerable<ScalefishClassModel> testClassComplexityResultsEnumerable)
    {
        var testClassComplexityResults = testClassComplexityResultsEnumerable.ToList();
        var tableBuilder = new StringBuilder();
        foreach (var testClassComplexityResult in testClassComplexityResults)
        {
            tableBuilder.AppendLine($"Namespace: {testClassComplexityResult.NameSpace}");
            tableBuilder.AppendLine($"Test Class: {testClassComplexityResult.TestClassName}");
            tableBuilder.AppendLine();
            var methodGroups = testClassComplexityResult
                .ScaleFishMethodModels
                .GroupBy(x => x.TestMethodName);

            foreach (var methodGroup in methodGroups)
            {
                tableBuilder.AppendLine($"### {methodGroup.Key}");
                tableBuilder.AppendLine();
                tableBuilder.AppendLine(methodGroup
                    .SelectMany(x => x.ScaleFishPropertyModels)
                    .ToStringTable(
                        new List<string>
                        {
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            ""
                        },
                        new List<string>
                        {
                            "Variable",
                            "BestFit",
                            "BigO",
                            "GoodnessOfFit",
                            "NextBest",
                            "NextBigO",
                            "NextBestGoodnessOfFit"
                        },
                        c => c.PropertyName,
                        c => c.ScaleFishModel.ScaleFishModelFunction.Name,
                        c => c.ScaleFishModel.ScaleFishModelFunction.OName,
                        c => c.ScaleFishModel.GoodnessOfFit,
                        c => c.ScaleFishModel.NextClosestScaleFishModelFunction.Name,
                        c => c.ScaleFishModel.NextClosestScaleFishModelFunction.OName,
                        c => c.ScaleFishModel.NextClosestGoodnessOfFit
                    ));
                tableBuilder.AppendLine();
            }
        }

        return tableBuilder.ToString();
    }

    private static void AppendResults(string typeName, IEnumerable<ICompiledTestCaseResult> compiledResults, StringBuilder stringBuilder)
    {
        foreach (var group in compiledResults.GroupBy(x => x.GroupingId))
        {
            if (group.Key is null) continue;
            stringBuilder.AppendLine();
            var n = group.Select(x => x.PerformanceRunResult?.SampleSize).Distinct().Single();
            if (n is null or 0) continue;

            var table = group.ToStringTable(
                typeName,
                new List<string>

                {
                    "",
                    "ms",
                    "ms",
                    "ms",
                    ""
                },
                new List<string>
                {
                    "Display Name",
                    "Mean",
                    "Median",
                    $"StdDev (N={n})",
                    "Variance"
                },
                u => u.TestCaseId!.DisplayName,
                u => u.PerformanceRunResult!.Mean,
                u => u.PerformanceRunResult!.Median,
                u => u.PerformanceRunResult!.StdDev,
                u => u.PerformanceRunResult!.Variance
            );

            stringBuilder.AppendLine(table);
        }
    }

    private static void AppendExceptions(IReadOnlyCollection<Exception?> exceptions, StringBuilder stringBuilder)
    {
        if (exceptions.Count > 0) stringBuilder.AppendLine(" ---- One or more Exceptions encountered ---- ");

        foreach (var exception in exceptions.Where(exception => exception is not null))
        {
            stringBuilder.AppendLine($"Exception: {exception?.Message}\r");
            if (exception?.StackTrace is not null) stringBuilder.AppendLine($"StackTrace:\r{exception.StackTrace}\r");
        }
    }
}