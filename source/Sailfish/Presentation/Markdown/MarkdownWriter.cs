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

    public async Task Present(IEnumerable<IExecutionSummary> results, string filePath, IRunSettings settings, CancellationToken cancellationToken)
    {
        var markdownStringTable = markdownTableConverter.ConvertToMarkdownTableString(results, result => result.Settings.AsMarkdown);
        if (!string.IsNullOrEmpty(markdownStringTable))
        {
            await fileIo.WriteToFile(markdownStringTable, filePath, cancellationToken).ConfigureAwait(false);
        }
    }
}