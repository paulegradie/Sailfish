using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteScalefishResultHandler : INotificationHandler<WriteCurrentScalefishResultCommand>
{
    public async Task Handle(WriteCurrentScalefishResultCommand notification, CancellationToken cancellationToken)
    {
        var output = notification.LocalOutputDirectory;
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }

        var fileName = DefaultFileSettings.AppendTagsToFilename(notification.DefaultFileName, notification.Tags);
        var filepath = Path.Join(output, fileName);

        await using var streamWriter = new StreamWriter(filepath);
        await streamWriter.WriteAsync(notification.ScalefishResultMarkdown).ConfigureAwait(false);
    }
}