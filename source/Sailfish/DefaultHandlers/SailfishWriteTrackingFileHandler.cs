using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteTrackingFileHandler : INotificationHandler<WriteCurrentTrackingFileCommand>
{
    public async Task Handle(WriteCurrentTrackingFileCommand notification, CancellationToken cancellationToken)
    {
        var output = notification.DefaultOutputDirectory;
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }

        var fileName = DefaultFileSettings.AppendTagsToFilename(notification.DefaultFileName, notification.Tags);
        var filePath = Path.Join(output, fileName);

        await using var streamWriter = new StreamWriter(filePath);
        await streamWriter.WriteAsync(notification.Content).ConfigureAwait(false);
    }
}