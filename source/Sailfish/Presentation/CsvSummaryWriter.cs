using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Presentation.CsvAndJson;

namespace Sailfish.Presentation;

/// <summary>
///     Writes the run's execution summaries to a CSV file. Previously the handler for the internal
///     <c>WriteToCsvNotification</c>; collapsed to a direct service call.
/// </summary>
internal interface ICsvSummaryWriter
{
    Task Write(List<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken);
}

internal class CsvSummaryWriter : ICsvSummaryWriter
{
    private readonly IPerformanceRunResultFileWriter _performanceRunResultFileWriter;
    private readonly IRunSettings _runSettings;

    public CsvSummaryWriter(IPerformanceRunResultFileWriter performanceRunResultFileWriter, IRunSettings runSettings)
    {
        _performanceRunResultFileWriter = performanceRunResultFileWriter;
        _runSettings = runSettings;
    }

    public async Task Write(List<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultPerformanceResultsFileNameStem(_runSettings.TimeStamp) + ".csv", _runSettings.Tags);
        var filePath = Path.Combine(_runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory, fileName);
        await _performanceRunResultFileWriter.WriteToFileAsCsv(executionSummaries, filePath, summary => summary.ExecutionSettings.AsCsv, cancellationToken)
            .ConfigureAwait(false);
    }
}
