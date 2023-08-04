using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sailfish.Analysis.ComplexityEstimation;
using Sailfish.Execution;
using Sailfish.Extensions.Methods;
using Sailfish.Statistics;

namespace Sailfish.Presentation;

public class MarkdownTableConverter : IMarkdownTableConverter
{
    public string ConvertToMarkdownTableString(IEnumerable<IExecutionSummary> executionSummaries,
        Func<IExecutionSummary, bool> summaryFilter)
    {
        var filteredSummaries = executionSummaries.Where(summaryFilter);
        return ConvertToMarkdownTableString(filteredSummaries);
    }

    public string ConvertToMarkdownTableString(IEnumerable<IExecutionSummary> executionSummaries)
    {
        var stringBuilder = new StringBuilder();
        foreach (var result in executionSummaries)
        {
            AppendHeader(result.Type.Name, stringBuilder);
            AppendResults(result.CompiledTestCaseResults, stringBuilder);

            var exceptions = result.CompiledTestCaseResults.SelectMany(x => x.Exceptions).ToList();
            AppendExceptions(exceptions, stringBuilder);
        }

        return stringBuilder.ToString();
    }

    private void AppendHeader(string typeName, StringBuilder stringBuilder)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"\r{typeName}\r");
        stringBuilder.AppendLine("-----------------------------------\r");
    }

    private void AppendResults(IEnumerable<ICompiledTestCaseResult> compiledResults, StringBuilder stringBuilder)
    {
        foreach (var group in compiledResults.GroupBy(x => x.GroupingId))
        {
            if (group.Key is null) continue;
            stringBuilder.AppendLine();
            var table = group.ToStringTable(
                new List<string>() { "", "ms", "ms", "ms", "" },
                u => u.TestCaseId!.DisplayName!,
                u => u.DescriptiveStatisticsResult!.Median,
                u => u.DescriptiveStatisticsResult!.Mean,
                u => u.DescriptiveStatisticsResult!.StdDev,
                u => u.DescriptiveStatisticsResult!.Variance
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


    public string ConvertComplexityResultToMarkdown(IEnumerable<ITestClassComplexityResult> testClassComplexityResultsEnumerable)
    {
        var testClassComplexityResults = testClassComplexityResultsEnumerable.ToList();
        var tableBuilder = new StringBuilder();
        foreach (var testClassComplexityResult in testClassComplexityResults)
        {
            tableBuilder.AppendLine($"{nameof(testClassComplexityResult.TestClassName)}: {testClassComplexityResult.TestClassName}");

            foreach (var testMethodComplexityResult in testClassComplexityResult.TestMethodComplexityResults)
            {
                var tableSection = testMethodComplexityResult.TestPropertyComplexityResults.ToStringTable(
                    // new[] { "", "", "" },
                    c => c.PropertyName,
                    c => c.ComplexityResult.ComplexityFunction,
                    c => c.ComplexityResult.GoodnessOfFit,
                    c => c.ComplexityResult.NextClosestComplexity,
                    c => c.ComplexityResult.NextClosestGoodnessOfFit
                );
                tableBuilder.AppendLine(tableSection);
            }
        }


        return tableBuilder.ToString();
    }
}