using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sailfish.Execution;
using Sailfish.Extensions.Methods;
using Sailfish.Statistics;

namespace Sailfish.Presentation;

public class MarkdownTableConverter : IMarkdownTableConverter
{
    public string ConvertToMarkdownTableString(IEnumerable<IExecutionSummary> executionSummaries, Func<IExecutionSummary, bool> summaryFilter)
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

    private void AppendExceptions(IReadOnlyCollection<Exception?> exceptions, StringBuilder stringBuilder)
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
}