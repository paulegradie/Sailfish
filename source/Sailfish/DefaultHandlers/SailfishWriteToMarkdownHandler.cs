using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Presentation;
using Sailfish.Presentation.Markdown;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteToMarkdownHandler : INotificationHandler<WriteToMarkDownCommand>
{
    private readonly IMarkdownWriter markdownWriter;

    public SailfishWriteToMarkdownHandler(IMarkdownWriter markdownWriter)
    {
        this.markdownWriter = markdownWriter;
    }

    public async Task Handle(WriteToMarkDownCommand notification, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultPerformanceFileNameStem(notification.TimeStamp) + ".md", notification.Tags);
        var filePath = Path.Combine(notification.OutputDirectory, fileName);
        await markdownWriter.Present(notification.Content, filePath, notification.Settings, cancellationToken).ConfigureAwait(false);
    }
}