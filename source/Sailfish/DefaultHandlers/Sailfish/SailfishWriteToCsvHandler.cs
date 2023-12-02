using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Presentation;
using Sailfish.Presentation.CsvAndJson;

namespace Sailfish.DefaultHandlers.Sailfish;

internal class SailfishWriteToCsvHandler : INotificationHandler<WriteToCsvNotification>
{
    private readonly IPerformanceRunResultFileWriter performanceRunResultFileWriter;
    private readonly IRunSettings runSettings;

    public SailfishWriteToCsvHandler(IPerformanceRunResultFileWriter performanceRunResultFileWriter, IRunSettings runSettings)
    {
        this.performanceRunResultFileWriter = performanceRunResultFileWriter;
        this.runSettings = runSettings;
    }

    public async Task Handle(WriteToCsvNotification notification, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultPerformanceResultsFileNameStem(runSettings.TimeStamp) + ".csv", runSettings.Tags);
        var filePath = Path.Combine(runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory, fileName);
        await performanceRunResultFileWriter.WriteToFileAsCsv(notification.ClassExecutionSummaries, filePath, summary => summary.ExecutionSettings.AsCsv, cancellationToken).ConfigureAwait(false);
    }
}