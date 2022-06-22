using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VeerPerforma.Presentation.Console;
using VeerPerforma.Statistics;
using VeerPerforma.Utils;

namespace VeerPerforma.Presentation.Markdown;

public class MarkdownWriter : IMarkdownWriter
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

    public async Task Present(List<CompiledResultContainer> results, string filePath)
    {
        foreach (var result in results)
        {
            if (result.Settings.AsMarkdown)
            {
                PrintHeader(result.Type.Name);
                PrintResults(result.CompiledResults);
                PrintExceptions(result.Exceptions);
            }
        }

        var fileString = stringBuilder.Build();
        if (!string.IsNullOrEmpty(fileString))
            await fileIo.WriteToFile(fileString, filePath, CancellationToken.None);
    }

    private void PrintHeader(string typeName)
    {
        stringBuilder.AppendLine();
        stringBuilder.AppendLine("\r-----------------------------------");
        stringBuilder.AppendLine($"\r{typeName}\r");
        stringBuilder.AppendLine("-----------------------------------\r");
    }

    private void PrintResults(List<CompiledResult> compiledResults)
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

    private void PrintExceptions(List<Exception> exceptions)
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