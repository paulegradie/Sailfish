using System;
using System.Collections.Generic;
using System.Linq;
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

    public string Present(List<ExecutionSummary> results)
    {
        foreach (var result in results)
        {
            if (result.Settings.AsConsole)
            {
                AppendHeader(result.Type.Name);
                AppendResults(result.CompiledResults);
                AppendExceptions(result.CompiledResults.Where(x => x.Exception is not null).Select(x => x.Exception).ToList());
            }
        }

        var output = stringBuilder.Build();
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

    private void AppendResults(List<CompiledResult> compiledResults)
    {
        foreach (var group in compiledResults.GroupBy(x => x.GroupingId))
        {
            if (group.Key is not null)
            {
                stringBuilder.AppendLine();
                var table = group.ToStringTable(
                    new List<string>() { "", "ms", "ms", "ms", "" },
                    u => u.DisplayName!,
                    u => u.TestCaseStatistics!.Median,
                    u => u.TestCaseStatistics!.Mean,
                    u => u.TestCaseStatistics!.StdDev,
                    u => u.TestCaseStatistics!.Variance
                );

                stringBuilder.AppendLine(table);
            }
        }
    }

    private void AppendExceptions(List<Exception?> exceptions)
    {
        if (exceptions.Count > 0)
            stringBuilder.AppendLine($" ---- One or more Exceptions encountered ---- ");
        foreach (var exception in exceptions)
        {
            if (exception is null) continue;
            stringBuilder.AppendLine($"Exception: {exception.Message}\r");
            if (exception.StackTrace is not null)
            {
                stringBuilder.AppendLine($"StackTrace:\r{exception.StackTrace}\r");
            }
        }
    }
}