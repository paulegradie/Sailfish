using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Presentation;
using Sailfish.Presentation.CsvAndJson;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.DefaultHandlers.Sailfish;

internal class SailfishWriteToCsvHandler(IPerformanceRunResultFileWriter performanceRunResultFileWriter, IRunSettings runSettings) : INotificationHandler<WriteToCsvNotification>
{
    private readonly IPerformanceRunResultFileWriter performanceRunResultFileWriter = performanceRunResultFileWriter;
    private readonly IRunSettings runSettings = runSettings;

    public async Task Handle(WriteToCsvNotification notification, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultPerformanceResultsFileNameStem(runSettings.TimeStamp) + ".csv", runSettings.Tags);
        var filePath = Path.Combine(runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory, fileName);
        await performanceRunResultFileWriter.WriteToFileAsCsv(notification.ClassExecutionSummaries, filePath, summary => summary.ExecutionSettings.AsCsv, cancellationToken)
            .ConfigureAwait(false);
    }
}