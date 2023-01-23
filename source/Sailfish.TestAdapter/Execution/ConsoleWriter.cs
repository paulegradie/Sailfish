using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Collections;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Sailfish.Execution;
using Sailfish.ExtensionMethods;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using Sailfish.Statistics;
using Serilog;
using Serilog.Core;

namespace Sailfish.TestAdapter.Execution;

internal class ConsoleWriter : IConsoleWriter
{
    private readonly IPresentationStringConstructor stringBuilder;
    private readonly IMessageLogger? messageLogger;
    private readonly Logger consoleLogger;

    public ConsoleWriter(IPresentationStringConstructor stringBuilder, IMessageLogger? messageLogger)
    {
        this.stringBuilder = stringBuilder;
        this.messageLogger = messageLogger;
        consoleLogger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
    }

    public string Present(IEnumerable<ExecutionSummary> results, OrderedDictionary<string, string>? tags = null)
    {
        foreach (var result in results.Where(result => result.Settings.AsConsole))
        {
            AppendHeader(result.Type.Name);
            AppendResults(result.CompiledResults);
            AppendExceptions(result.CompiledResults.Where(x => x.Exception is not null).Select(x => x.Exception).ToList());
        }

        var output = stringBuilder.Build();

        messageLogger?.SendMessage(TestMessageLevel.Informational, output);
        consoleLogger.Information(output);
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