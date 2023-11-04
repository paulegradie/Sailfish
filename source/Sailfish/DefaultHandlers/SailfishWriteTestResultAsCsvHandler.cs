using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private.CsvMaps;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteTestResultAsCsvHandler : INotificationHandler<WriteTestResultsAsCsvNotification>
{
    private readonly IFileIo fileIo;

    public SailfishWriteTestResultAsCsvHandler(IFileIo fileIo)
    {
        this.fileIo = fileIo;
    }

    public async Task Handle(WriteTestResultsAsCsvNotification notification, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings
            .AppendTagsToFilename(
                DefaultFileSettings.DefaultSaildiffCsvFileName(notification.TimeStamp, notification.SailDiffSettings.TestType),
                notification.Tags);
        var outputPath = Path.Join(notification.OutputDirectory, fileName);
        if (notification.CsvFormat.Any())
        {
            await fileIo
                .WriteDataAsCsvToFile<SailDiffWriteAsCsvMap, IEnumerable<TestCaseResults>>(
                    notification.CsvFormat,
                    outputPath,
                    cancellationToken).ConfigureAwait(false);
        }
    }
}