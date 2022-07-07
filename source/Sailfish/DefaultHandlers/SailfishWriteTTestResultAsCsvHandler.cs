using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation;
using Sailfish.Presentation.Csv;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteTTestResultAsCsvHandler : INotificationHandler<WriteTTestResultAsCsvCommand>
{
    private readonly ITTestResultCsvWriter testResultCsvWriter;

    public SailfishWriteTTestResultAsCsvHandler(ITTestResultCsvWriter testResultCsvWriter)
    {
        this.testResultCsvWriter = testResultCsvWriter;
    }

    public async Task Handle(WriteTTestResultAsCsvCommand notification, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultTTestCsvFileName(notification.TimeStamp), notification.Tags);
        var outputPath = Path.Join(notification.OutputDirectory, fileName);
        if (notification.CsvRows.Count > 0)
        {
            await testResultCsvWriter.WriteToFile(notification.CsvRows, outputPath, cancellationToken);
        }
    }
}