using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper;
using MediatR;
using Sailfish.Contracts.Private.CsvMaps;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Presentation;

namespace Sailfish.DefaultHandlers.SailDiff;

internal class SailDiffAnalysisCompleteNotificationHandler : INotificationHandler<SailDiffAnalysisCompleteNotification>
{
    private readonly IRunSettings runSettings;

    public SailDiffAnalysisCompleteNotificationHandler(IRunSettings runSettings)
    {
        this.runSettings = runSettings;
    }

    public async Task Handle(SailDiffAnalysisCompleteNotification notification, CancellationToken cancellationToken)
    {
        await WriteMarkdownTable(notification.ResultsAsMarkdown, cancellationToken);
        await WriteCsvFile(notification.TestCaseResults.ToList(), cancellationToken);
    }

    private async Task WriteMarkdownTable(string markdownTable, CancellationToken cancellationToken)
    {
        var filename = DefaultFileSettings.AppendTagsToFilename(
            DefaultFileSettings.DefaultSaildiffMarkdownFileName(runSettings.TimeStamp, runSettings.SailDiffSettings.TestType),
            runSettings.Tags);
        var filePath = Path.Join(runSettings.LocalOutputDirectory, filename);
        if (!string.IsNullOrEmpty(markdownTable))
        {
            if (Directory.Exists(filePath)) throw new IOException("Cannot write to a directory");

            await File.WriteAllTextAsync(filePath, markdownTable, cancellationToken).ConfigureAwait(false);
            File.SetAttributes(filePath, FileAttributes.ReadOnly);
        }
    }

    private async Task WriteCsvFile(IReadOnlyCollection<SailDiffResult> testCaseResults, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings
            .AppendTagsToFilename(
                DefaultFileSettings.DefaultSaildiffCsvFileName(
                    runSettings.TimeStamp,
                    runSettings.SailDiffSettings.TestType),
                runSettings.Tags);
        var filePath = Path.Join(runSettings.LocalOutputDirectory, fileName);
        if (testCaseResults.Count != 0)
        {
            await using var writer = new StreamWriter(filePath);
            await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.Context.RegisterClassMap<SailDiffWriteAsCsvMap>();
            await csv.WriteRecordsAsync(testCaseResults, cancellationToken).ConfigureAwait(false);
        }
    }
}