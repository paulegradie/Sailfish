using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers;

public class SailfishWriteTTestMarkdownResultHandler : INotificationHandler<WriteTTestResultAsMarkdownCommand>
{
    private readonly IFileIo fileIo;

    public SailfishWriteTTestMarkdownResultHandler(IFileIo fileIo)
    {
        this.fileIo = fileIo;
    }

    public async Task Handle(WriteTTestResultAsMarkdownCommand notification, CancellationToken cancellationToken)
    {
        var filename = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultTTestMarkdownFileName(notification.TimeStamp), notification.Tags);
        var outputPath = Path.Join(notification.OutputDirectory, filename);
        if (!string.IsNullOrEmpty(notification.Content))
        {
            await fileIo.WriteToFile(notification.Content, outputPath, cancellationToken).ConfigureAwait(false);
        }

        System.Console.WriteLine(notification.Content);
    }
}