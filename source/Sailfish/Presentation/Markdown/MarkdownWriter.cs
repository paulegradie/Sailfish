using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Execution;

namespace Sailfish.Presentation.Markdown;

internal interface IMarkdownWriter
{
    Task Write(IEnumerable<IClassExecutionSummary> result, string filePath, CancellationToken cancellationToken);
    Task WriteEnhanced(IEnumerable<IClassExecutionSummary> result, string filePath, CancellationToken cancellationToken);
}

internal class MarkdownWriter : IMarkdownWriter
{
    private readonly IMarkdownTableConverter _markdownTableConverter;

    public MarkdownWriter(IMarkdownTableConverter markdownTableConverter)
    {
        _markdownTableConverter = markdownTableConverter;
    }

    public async Task Write(IEnumerable<IClassExecutionSummary> results, string filePath, CancellationToken cancellationToken)
    {
        var markdownStringTable = _markdownTableConverter.ConvertToMarkdownTableString(results, result => result.ExecutionSettings.AsMarkdown);
        if (!string.IsNullOrEmpty(markdownStringTable))
        {
            if (Directory.Exists(filePath)) throw new IOException("Cannot write to a directory");

            await File.WriteAllTextAsync(filePath, markdownStringTable, cancellationToken).ConfigureAwait(false);
            File.SetAttributes(filePath, FileAttributes.ReadOnly);
        }
    }

    public async Task WriteEnhanced(IEnumerable<IClassExecutionSummary> results, string filePath, CancellationToken cancellationToken)
    {
        // Try to use enhanced formatting if available
        var markdownContent = _markdownTableConverter.ConvertToEnhancedMarkdownTableString(results, result => result.ExecutionSettings.AsMarkdown);

        if (!string.IsNullOrEmpty(markdownContent))
        {
            if (Directory.Exists(filePath)) throw new IOException("Cannot write to a directory");

            await File.WriteAllTextAsync(filePath, markdownContent, cancellationToken).ConfigureAwait(false);
            File.SetAttributes(filePath, FileAttributes.ReadOnly);
        }
    }
}