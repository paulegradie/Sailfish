using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Presentation;
using Sailfish.Presentation.Markdown;

namespace Sailfish.DefaultHandlers.Sailfish;

internal class SailfishWriteToMarkdownHandler : INotificationHandler<WriteToMarkDownNotification>
{
    private readonly IMarkdownWriter markdownWriter;
    private readonly IRunSettings runSettings;

    public SailfishWriteToMarkdownHandler(IMarkdownWriter markdownWriter, IRunSettings runSettings)
    {
        this.markdownWriter = markdownWriter;
        this.runSettings = runSettings;
    }

    public async Task Handle(WriteToMarkDownNotification notification, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultPerformanceResultsFileNameStem(runSettings.TimeStamp) + ".md", runSettings.Tags);
        var filePath = Path.Combine(runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory, fileName);
        await markdownWriter.Write(notification.ClassExecutionSummaries, filePath, cancellationToken).ConfigureAwait(false);
    }
}