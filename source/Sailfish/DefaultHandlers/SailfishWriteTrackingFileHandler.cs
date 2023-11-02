using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Presentation;
using Serilog;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteTrackingFileHandler : INotificationHandler<WriteCurrentTrackingFileCommand>
{
    private readonly ITrackingFileSerialization trackingFileSerialization;
    private readonly IRunSettings runSettings;
    private readonly ILogger logger;

    public SailfishWriteTrackingFileHandler(ITrackingFileSerialization trackingFileSerialization, IRunSettings runSettings, ILogger logger)
    {
        this.trackingFileSerialization = trackingFileSerialization;
        this.runSettings = runSettings;
        this.logger = logger;
    }

    public async Task Handle(WriteCurrentTrackingFileCommand notification, CancellationToken cancellationToken)
    {
        var output = runSettings.GetRunSettingsTrackingDirectoryPath();
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }

        var fileName = DefaultFileSettings.AppendTagsToFilename(notification.DefaultFileName, notification.Tags);
        var filePath = Path.Join(output, fileName);

        var successfulSummaries = notification.ClassExecutionSummaries.Select(x => x.FilterForSuccessfulTestCases());
        foreach (var failedSummary in notification.ClassExecutionSummaries.SelectMany(x => x.GetFailedTestCases()))
        {
            logger.Warning(failedSummary.Exception, "Test case exception encountered");
        }

        var serialized = trackingFileSerialization.Serialize(successfulSummaries);

        await using var streamWriter = new StreamWriter(filePath);
        await streamWriter.WriteAsync(serialized).ConfigureAwait(false);
    }
}