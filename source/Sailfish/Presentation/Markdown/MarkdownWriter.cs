using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public;
using Sailfish.Execution;
using Sailfish.ExtensionMethods;
using Sailfish.Statistics;

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

    public async Task Present(List<ExecutionSummary> results, string filePath, RunSettings settings, CancellationToken cancellationToken)
    {
        foreach (var result in results.Where(result => result.Settings.AsMarkdown))
        {
            AppendHeader(result.Type.Name);
            AppendResults(result.CompiledResults);
            AppendExceptions(result.CompiledResults.Where(x => x.Exception is not null).Select(x => x.Exception).ToList(), settings.Debug);
        }

        var fileString = stringBuilder.Build();
        if (!string.IsNullOrEmpty(fileString))
        {
            await fileIo.WriteToFile(fileString, filePath, cancellationToken).ConfigureAwait(false);
        }
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
                u => u.DisplayName!,
                u => u.DescriptiveStatisticsResult!.Median,
                u => u.DescriptiveStatisticsResult!.Mean,
                u => u.DescriptiveStatisticsResult!.StdDev,
                u => u.DescriptiveStatisticsResult!.Variance
            );

            stringBuilder.AppendLine(table);
        }
    }

    // // TODO: allow arg to dictate this
    // public readonly bool debug = true;

    private void AppendExceptions(IReadOnlyCollection<Exception?> exceptions, bool debug)
    {
        if (exceptions.Count > 0)
        {
            stringBuilder.AppendLine($" ---- One or more Exceptions encountered ---- ");
        }

        foreach (var exception in exceptions.Where(exception => exception is not null))
        {
            stringBuilder.AppendLine($"Exception: {exception?.Message}\r");
            if (exception?.StackTrace is not null && debug)
            {
                stringBuilder.AppendLine($"StackTrace:\r{exception.StackTrace}\r");
            }
        }
    }
}