using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Collections;
using Sailfish.Execution;
using Sailfish.ExtensionMethods;
using Sailfish.Statistics;

namespace Sailfish.Presentation.Console;

internal class ConsoleWriter : IConsoleWriter
{
    private readonly IPresentationStringConstructor stringBuilder;

    public ConsoleWriter(IPresentationStringConstructor stringBuilder)
    {
        this.stringBuilder = stringBuilder;
    }

    public string Present(IEnumerable<ExecutionSummary> results, OrderedDictionary<string, string> tags)
    {
        foreach (var result in results.Where(result => result.Settings.AsConsole))
        {
            AppendHeader(result.Type.Name);
            AppendResults(result.CompiledResults);
            AppendExceptions(result.CompiledResults.Where(x => x.Exception is not null).Select(x => x.Exception).ToList());
        }

        var output = stringBuilder.Build();

        if (tags.Any()) System.Console.WriteLine($"{Environment.NewLine}Tags:");
        foreach (var (key, value) in tags)
        {
            System.Console.WriteLine($"{key}: {value}");
        }

        System.Console.WriteLine(output);
        return output;
    }

    private void AppendHeader(string typeName)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("\r-----------------------------------");
        stringBuilder.AppendLine($"\r{typeName}\r");
        stringBuilder.AppendLine("-----------------------------------\r");
    }

    private void AppendResults(IEnumerable<CompiledResult> compiledResults)
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

    private void AppendExceptions(IReadOnlyCollection<Exception?> exceptions)
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