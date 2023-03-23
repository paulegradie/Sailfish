using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Contracts.Public.CsvMaps;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteTestResultAsCsvHandler : INotificationHandler<WriteTestResultsAsCsvCommand>
{
    private readonly IFileIo fileIo;

    public SailfishWriteTestResultAsCsvHandler(IFileIo fileIo)
    {
        this.fileIo = fileIo;
    }

    public async Task Handle(WriteTestResultsAsCsvCommand notification, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings
            .AppendTagsToFilename(
                DefaultFileSettings.DefaultTTestCsvFileName(notification.TimeStamp, notification.TestSettings.TestType),
                notification.Tags);
        var outputPath = Path.Join(notification.OutputDirectory, fileName);
        if (notification.CsvFormat.Any())
        {
            await fileIo
                .WriteDataAsCsvToFile<DescriptiveStatisticsResultCsvMap, IEnumerable<TestCaseResults>>(
                    notification.CsvFormat,
                    outputPath,
                    cancellationToken).ConfigureAwait(false);
        }
    }
}