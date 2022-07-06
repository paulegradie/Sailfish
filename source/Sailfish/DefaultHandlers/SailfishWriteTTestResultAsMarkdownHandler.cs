using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation;
using Sailfish.Utils;

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
        var outputPath = Path.Join(notification.OutputDirectory, DefaultFileSettings.DefaultTTestMarkdownFileName(notification.TimeStamp));
        if (!string.IsNullOrEmpty(notification.Content))
        {
            await fileIo.WriteToFile(notification.Content, outputPath, cancellationToken);
        }

        System.Console.WriteLine(notification.Content);
    }
}