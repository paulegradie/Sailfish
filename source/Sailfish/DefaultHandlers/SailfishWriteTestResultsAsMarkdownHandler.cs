using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteTestResultsAsMarkdownHandler : INotificationHandler<WriteTestResultsAsMarkdownCommand>
{
    private readonly IFileIo fileIo;

    public SailfishWriteTestResultsAsMarkdownHandler(IFileIo fileIo)
    {
        this.fileIo = fileIo;
    }

    public async Task Handle(WriteTestResultsAsMarkdownCommand notification, CancellationToken cancellationToken)
    {
        var filename = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultTTestMarkdownFileName(notification.TimeStamp, notification.TestSettings.TestType), notification.Tags);
        var outputPath = Path.Join(notification.OutputDirectory, filename);
        if (!string.IsNullOrEmpty(notification.MarkdownTable))
        {
            await fileIo.WriteToFile(notification.MarkdownTable, outputPath, cancellationToken).ConfigureAwait(false);
        }
    }
}