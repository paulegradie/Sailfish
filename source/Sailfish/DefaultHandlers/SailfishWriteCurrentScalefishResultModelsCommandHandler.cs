using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteCurrentScalefishResultModelsCommandHandler : INotificationHandler<WriteCurrentScalefishResultModelsCommand>
{
    public async Task Handle(WriteCurrentScalefishResultModelsCommand notification, CancellationToken cancellationToken)
    {
        var output = notification.LocalOutputDirectory;
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }

        var fileName = DefaultFileSettings.AppendTagsToFilename(notification.DefaultFileName, notification.Tags);
        var filepath = Path.Join(output, fileName);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true // For pretty-printing the JSON
        };
        var serializedResults = JsonSerializer.Serialize(notification.TestClassComplexityResults, options);

        await using var streamWriter = new StreamWriter(filepath);
        await streamWriter.WriteAsync(serializedResults).ConfigureAwait(false);
    }
}