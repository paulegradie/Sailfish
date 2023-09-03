using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Execution;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteTrackingFileHandler : INotificationHandler<WriteCurrentTrackingFileCommand>
{
    private readonly ITrackingFileSerialization trackingFileSerialization;

    public SailfishWriteTrackingFileHandler(ITrackingFileSerialization trackingFileSerialization)
    {
        this.trackingFileSerialization = trackingFileSerialization;
    }

    public async Task Handle(WriteCurrentTrackingFileCommand notification, CancellationToken cancellationToken)
    {
        var output = notification.LocalOutputDirectory;
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }

        var fileName = DefaultFileSettings.AppendTagsToFilename(notification.DefaultFileName, notification.Tags);
        var filePath = Path.Join(output, fileName);


        if (AnyExceptions(notification.ExecutionSummaries))
        {
            return;
        }

        var trackingFormattedExecutionSummaries = notification.ExecutionSummaries.ToTrackingFormat();
        var serialized = trackingFileSerialization.Serialize(trackingFormattedExecutionSummaries);

        await using var streamWriter = new StreamWriter(filePath);
        await streamWriter.WriteAsync(serialized).ConfigureAwait(false);
    }

    private bool AnyExceptions(IEnumerable<IExecutionSummary> notificationExecutionSummaries)
    {
        var allResults = notificationExecutionSummaries.SelectMany(x => x.CompiledTestCaseResults).SelectMany(x => x.Exceptions).ToList();
        return allResults.Any();
    }
}