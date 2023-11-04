using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteScalefishResultHandler : INotificationHandler<WriteCurrentScalefishResultNotification>
{
    private readonly IRunSettings runSettings;

    public SailfishWriteScalefishResultHandler(IRunSettings runSettings)
    {
        this.runSettings = runSettings;
    }

    public async Task Handle(WriteCurrentScalefishResultNotification notification, CancellationToken cancellationToken)
    {
        var output = runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory;
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }

        var fileName = DefaultFileSettings.AppendTagsToFilename(notification.DefaultFileName, runSettings.Tags);
        var filepath = Path.Join(output, fileName);

        await using var streamWriter = new StreamWriter(filepath);
        await streamWriter.WriteAsync(notification.ScalefishResultMarkdown).ConfigureAwait(false);
    }
}