using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private.CsvMaps;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers.SailDiff;

internal class SailDiffAnalysisCompleteNotificationHandler : INotificationHandler<SailDiffAnalysisCompleteNotification>
{
    private readonly IFileIo fileIo;
    private readonly IRunSettings runSettings;

    public SailDiffAnalysisCompleteNotificationHandler(IFileIo fileIo, IRunSettings runSettings)
    {
        this.fileIo = fileIo;
        this.runSettings = runSettings;
    }

    public async Task Handle(SailDiffAnalysisCompleteNotification notification, CancellationToken cancellationToken)
    {
        await WriteMarkdownTable(notification.ResultsAsMarkdown, cancellationToken);
        await WriteCsvFile(notification.TestCaseResults, cancellationToken);
    }

    private async Task WriteMarkdownTable(string markdownTable, CancellationToken cancellationToken)
    {
        var filename = DefaultFileSettings.AppendTagsToFilename(
            DefaultFileSettings.DefaultSaildiffMarkdownFileName(runSettings.TimeStamp, runSettings.SailDiffSettings.TestType),
            runSettings.Tags);
        var outputPath = Path.Join(runSettings.LocalOutputDirectory, filename);
        if (!string.IsNullOrEmpty(markdownTable))
        {
            await fileIo.WriteStringToFile(markdownTable, outputPath, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WriteCsvFile(IEnumerable<TestCaseResults> testCaseResults, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings
            .AppendTagsToFilename(
                DefaultFileSettings.DefaultSaildiffCsvFileName(
                    runSettings.TimeStamp,
                    runSettings.SailDiffSettings.TestType),
                runSettings.Tags);
        var outputPath = Path.Join(runSettings.LocalOutputDirectory, fileName);
        if (testCaseResults.Any())
        {
            await fileIo
                .WriteDataAsCsvToFile<SailDiffWriteAsCsvMap, IEnumerable<TestCaseResults>>(
                    testCaseResults,
                    outputPath,
                    cancellationToken).ConfigureAwait(false);
        }
    }
}