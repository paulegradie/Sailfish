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
    private readonly IPerformanceRunResultFileWriter _performanceRunResultFileWriter;
    private readonly IRunSettings _runSettings;

    public SailfishWriteToCsvHandler(IPerformanceRunResultFileWriter performanceRunResultFileWriter, IRunSettings runSettings)
    {
        _performanceRunResultFileWriter = performanceRunResultFileWriter;
        _runSettings = runSettings;
    }

    public async Task Handle(WriteToCsvNotification notification, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultPerformanceResultsFileNameStem(_runSettings.TimeStamp) + ".csv", _runSettings.Tags);
        var filePath = Path.Combine(_runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory, fileName);
        await _performanceRunResultFileWriter.WriteToFileAsCsv(notification.ClassExecutionSummaries, filePath, summary => summary.ExecutionSettings.AsCsv, cancellationToken)
            .ConfigureAwait(false);
    }
}