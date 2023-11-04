using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteCurrentScalefishResultModelsCommandHandler : INotificationHandler<WriteCurrentScalefishResultModelsNotification>
{
    private readonly IRunSettings runSettings;

    public SailfishWriteCurrentScalefishResultModelsCommandHandler(IRunSettings runSettings)
    {
        this.runSettings = runSettings;
    }

    public async Task Handle(WriteCurrentScalefishResultModelsNotification notification, CancellationToken cancellationToken)
    {
        var output = runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory;
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }

        var fileName = DefaultFileSettings.AppendTagsToFilename(notification.DefaultFileName, runSettings.Tags);
        var filepath = Path.Join(output, fileName);

        var serializedResults = SailfishSerializer.Serialize(notification.TestClassComplexityResults);

        await using var streamWriter = new StreamWriter(filepath);
        await streamWriter.WriteAsync(serializedResults).ConfigureAwait(false);
    }
}