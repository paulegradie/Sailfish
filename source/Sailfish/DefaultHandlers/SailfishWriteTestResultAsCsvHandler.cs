using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Public.Commands;
using Sailfish.Presentation;
using Sailfish.Presentation.Csv;

namespace Sailfish.DefaultHandlers;

internal class SailfishWriteTestResultAsCsvHandler : INotificationHandler<WriteTestResultsAsCsvCommand>
{
    private readonly ITestResultsCsvWriter testResultsCsvWriter;

    public SailfishWriteTestResultAsCsvHandler(ITestResultsCsvWriter testResultsCsvWriter)
    {
        this.testResultsCsvWriter = testResultsCsvWriter;
    }

    public async Task Handle(WriteTestResultsAsCsvCommand notification, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultTTestCsvFileName(notification.TimeStamp, notification.TestSettings.TestType), notification.Tags);
        var outputPath = Path.Join(notification.OutputDirectory, fileName);
        if (notification.CsvFormat.Count > 0)
        {
            await testResultsCsvWriter.WriteToFile(notification.CsvFormat, outputPath, cancellationToken).ConfigureAwait(false);
        }
    }
}