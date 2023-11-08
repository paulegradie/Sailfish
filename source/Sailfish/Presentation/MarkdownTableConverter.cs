using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Execution;
using Sailfish.Extensions.Methods;
using Sailfish.Statistics;

namespace Sailfish.Presentation;

public class MarkdownTableConverter : IMarkdownTableConverter
{
    public string ConvertToMarkdownTableString(
        IEnumerable<IClassExecutionSummary> executionSummaries,
        Func<IClassExecutionSummary, bool> summaryFilter)
    {
        var filteredSummaries = executionSummaries.Where(summaryFilter);
        return ConvertToMarkdownTableString(filteredSummaries);
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

    private static void AppendResults(string typeName, IEnumerable<ICompiledTestCaseResult> compiledResults, StringBuilder stringBuilder)
    {
        foreach (var group in compiledResults.GroupBy(x => x.GroupingId))
        {
            if (group.Key is null) continue;
            stringBuilder.AppendLine();
            var n = group.Select(x => x.PerformanceRunResult?.SampleSize).Distinct().Single();
            if (n is null or 0)
            {
                continue;
            }

            var table = group.ToStringTable(
                typeName,
                new List<string>() { "", "ms", "ms", "ms", "" },
                new List<string> { "Display Name", "Mean", "Median", $"StdDev (N={n})", "Variance" },
                u => u.TestCaseId!.DisplayName!,
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
        if (exceptions.Count > 0)
        {
            stringBuilder.AppendLine($" ---- One or more Exceptions encountered ---- ");
        }

        foreach (var exception in exceptions.Where(exception => exception is not null))
        {
            stringBuilder.AppendLine($"Exception: {exception?.Message}\r");
            if (exception?.StackTrace is not null)
            {
                stringBuilder.AppendLine($"StackTrace:\r{exception.StackTrace}\r");
            }
        }
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
                tableBuilder.AppendLine(methodGroup
                    .SelectMany(x => x.ScaleFishPropertyModels)
                    .ToStringTable(
                        new List<string>() { "", "", "", "", "", "", "" },
                        new List<string>() { "Variable", "BestFit", "BigO", "GoodnessOfFit", "NextBest", "NextBigO", "NextBestGoodnessOfFit" },
                        c => c.PropertyName,
                        c => c.ScalefishModel.ScaleFishModelFunction.Name,
                        c => c.ScalefishModel.ScaleFishModelFunction.OName,
                        c => c.ScalefishModel.GoodnessOfFit,
                        c => c.ScalefishModel.NextClosestScaleFishModelFunction.Name,
                        c => c.ScalefishModel.NextClosestScaleFishModelFunction.OName,
                        c => c.ScalefishModel.NextClosestGoodnessOfFit
                    ));
            }
        }

        return tableBuilder.ToString();
    }
}