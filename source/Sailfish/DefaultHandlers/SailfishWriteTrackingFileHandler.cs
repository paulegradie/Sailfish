using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;

namespace Sailfish.DefaultHandlers;

public class SailfishWriteTrackingFileHandler : INotificationHandler<WriteCurrentTrackingFileCommand>
{
    public async Task Handle(WriteCurrentTrackingFileCommand notification, CancellationToken cancellationToken)
    {
        var output = notification.DefaultOutputDirectory;
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }

        var filePath = Path.Join(output, notification.DefaultFileName);

        using var streamWriter = new StreamWriter(filePath);
        await streamWriter.WriteAsync(notification.Content);
    }
}