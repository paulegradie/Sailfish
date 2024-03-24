using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Presentation.Markdown;

internal interface IMarkdownWriter
{
    Task Write(IEnumerable<IClassExecutionSummary> result, string filePath, CancellationToken cancellationToken);
}

internal class MarkdownWriter(IMarkdownTableConverter markdownTableConverter) : IMarkdownWriter
{
    private readonly IMarkdownTableConverter markdownTableConverter = markdownTableConverter;

    public async Task Write(IEnumerable<IClassExecutionSummary> results, string filePath, CancellationToken cancellationToken)
    {
        var markdownStringTable = markdownTableConverter.ConvertToMarkdownTableString(results, result => result.ExecutionSettings.AsMarkdown);
        if (!string.IsNullOrEmpty(markdownStringTable))
        {
            if (Directory.Exists(filePath)) throw new IOException("Cannot write to a directory");

            await File.WriteAllTextAsync(filePath, markdownStringTable, cancellationToken).ConfigureAwait(false);
            File.SetAttributes(filePath, FileAttributes.ReadOnly);
        }
    }
}