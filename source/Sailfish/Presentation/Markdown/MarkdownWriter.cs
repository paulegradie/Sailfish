using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public;
using Sailfish.Execution;

namespace Sailfish.Presentation.Markdown;

internal class MarkdownWriter : IMarkdownWriter
{
    private readonly IFileIo fileIo;
    private readonly IMarkdownTableConverter markdownTableConverter;

    public MarkdownWriter(
        IFileIo fileIo,
        IMarkdownTableConverter markdownTableConverter)
    {
        this.fileIo = fileIo;
        this.markdownTableConverter = markdownTableConverter;
    }

    public async Task Write(IEnumerable<IClassExecutionSummary> results, string filePath, CancellationToken cancellationToken)
    {
        var markdownStringTable = markdownTableConverter.ConvertToMarkdownTableString(results, result => result.ExecutionSettings.AsMarkdown);
        if (!string.IsNullOrEmpty(markdownStringTable))
        {
            await fileIo.WriteStringToFile(markdownStringTable, filePath, cancellationToken).ConfigureAwait(false);
        }
    }
}