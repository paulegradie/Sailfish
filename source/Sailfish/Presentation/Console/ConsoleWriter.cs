using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Statistics;
using Sailfish.Utils;

namespace Sailfish.Presentation.Console;

public class ConsoleWriter : IConsoleWriter
{
    private readonly IPresentationStringConstructor stringBuilder;

    public ConsoleWriter(IPresentationStringConstructor stringBuilder)
    {
        this.stringBuilder = stringBuilder;
    }

    public string Present(List<CompiledResultContainer> results)
    {
        foreach (var container in results)
        {
            if (container.Settings.AsConsole)
            {
                AddHeader(container.Type.Name);
                AddResults(container.CompiledResults);
                AddExceptions(container.Exceptions);
            }
        }

        var output = stringBuilder.Build();
        System.Console.WriteLine(output);
        return output;
    }

    private void AddHeader(string typeName)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("\r-----------------------------------");
        stringBuilder.AppendLine($"\r{typeName}\r");
        stringBuilder.AppendLine("-----------------------------------\r");
    }

    private void AddResults(List<CompiledResult> compiledResults)
    {
        foreach (var group in compiledResults.GroupBy(x => x.GroupingId))
        {
            stringBuilder.AppendLine();
            var table = group.ToStringTable(
                new List<string>() { "", "ms", "ms", "ms", "" },
                u => u.DisplayName,
                u => u.TestCaseStatistics.Median,
                u => u.TestCaseStatistics.Mean,
                u => u.TestCaseStatistics.StdDev,
                u => u.TestCaseStatistics.Variance
            );

            stringBuilder.AppendLine(table);
        }
    }

    private void AddExceptions(List<Exception> exceptions)
    {
        if (exceptions.Count > 0)
            stringBuilder.AppendLine($" ---- One or more Exceptions encountered ---- ");
        foreach (var exception in exceptions)
        {
            stringBuilder.AppendLine($"Exception: {exception.Message}\r");
            if (exception.StackTrace is not null)
            {
                stringBuilder.AppendLine($"StackTrace:\r{exception.StackTrace}\r");
            }
        }
    }
}