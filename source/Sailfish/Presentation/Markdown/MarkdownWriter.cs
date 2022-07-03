using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord;
using Sailfish.ExtensionMethods;
using Sailfish.Presentation.Console;
using Sailfish.Statistics;
using Sailfish.Utils;

namespace Sailfish.Presentation.Markdown;

internal class MarkdownWriter : IMarkdownWriter
{
    private readonly IFileIo fileIo;
    private readonly IPresentationStringConstructor stringBuilder;

    public MarkdownWriter(
        IFileIo fileIo,
        IPresentationStringConstructor stringBuilder)
    {
        this.fileIo = fileIo;
        this.stringBuilder = stringBuilder;
    }

    public async Task Present(List<ExecutionSummary> results, string filePath)
    {
        foreach (var result in results)
        {
            if (result.Settings.AsMarkdown)
            {
                AppendHeader(result.Type.Name);
                AppendResults(result.CompiledResults);
                AppendExceptions(result.CompiledResults.Where(x => x.Exception is not null).Select(x => x.Exception).ToList());
            }
        }

        var fileString = stringBuilder.Build();
        if (!string.IsNullOrEmpty(fileString))
        {
            await fileIo.WriteToFile(fileString, filePath, CancellationToken.None);
        }
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

    public bool debug = true;

    private void AppendExceptions(List<Exception?> exceptions)
    {
        if (exceptions.Count > 0)
            stringBuilder.AppendLine($" ---- One or more Exceptions encountered ---- ");
        foreach (var exception in exceptions)
        {
            if (exception is null) continue;
            stringBuilder.AppendLine($"Exception: {exception.Message}\r");
            if (exception.StackTrace is not null && debug)
            {
                stringBuilder.AppendLine($"StackTrace:\r{exception.StackTrace}\r");
            }
        }
    }
}