using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Presentation;
using Sailfish.Presentation.Markdown;

namespace Sailfish.DefaultHandlers.Sailfish;

internal class SailfishWriteToMarkdownHandler(IMarkdownWriter markdownWriter, IRunSettings runSettings) : INotificationHandler<WriteToMarkDownNotification>
{
    private readonly IMarkdownWriter markdownWriter = markdownWriter;
    private readonly IRunSettings runSettings = runSettings;

    public async Task Handle(WriteToMarkDownNotification notification, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultPerformanceResultsFileNameStem(runSettings.TimeStamp) + ".md", runSettings.Tags);
        var outputDirectory = runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory;
        if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

        var filePath = Path.Combine(outputDirectory, fileName);

        // Try to use enhanced formatting if available, otherwise fall back to legacy
        try
        {
            await markdownWriter.WriteEnhanced(notification.ClassExecutionSummaries, filePath, cancellationToken).ConfigureAwait(false);
        }
        catch (System.NotImplementedException)
        {
            // Fallback to legacy formatting if enhanced is not implemented
            await markdownWriter.Write(notification.ClassExecutionSummaries, filePath, cancellationToken).ConfigureAwait(false);
        }
    }
}