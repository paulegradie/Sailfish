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
        var output = Path.Combine(notification.DefaultOutputDirectory, "tracking_output");
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }

        using var streamWriter = new StreamWriter(notification.DefaultFileName);
        await streamWriter.WriteAsync(notification.Content);
    }
}