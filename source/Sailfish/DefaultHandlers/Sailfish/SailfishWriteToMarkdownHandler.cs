using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Contracts.Private;
using Sailfish.Contracts.Public.Models;
using Sailfish.Presentation;
using Sailfish.Presentation.Markdown;

namespace Sailfish.DefaultHandlers.Sailfish;

internal class SailfishWriteToMarkdownHandler : INotificationHandler<WriteToMarkDownNotification>
{
    private readonly IMarkdownWriter _markdownWriter;
    private readonly IRunSettings _runSettings;

    public SailfishWriteToMarkdownHandler(IMarkdownWriter markdownWriter, IRunSettings runSettings)
    {
        _markdownWriter = markdownWriter;
        _runSettings = runSettings;
    }

    public async Task Handle(WriteToMarkDownNotification notification, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultPerformanceResultsFileNameStem(_runSettings.TimeStamp) + ".md", _runSettings.Tags);
        var outputDirectory = _runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory;
        if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

        var filePath = Path.Combine(outputDirectory, fileName);

        // Try to use enhanced formatting if available, otherwise fall back to legacy
        try
        {
            await _markdownWriter.WriteEnhanced(notification.ClassExecutionSummaries, filePath, cancellationToken).ConfigureAwait(false);
        }
        catch (System.NotImplementedException)
        {
            // Fallback to legacy formatting if enhanced is not implemented
            await _markdownWriter.Write(notification.ClassExecutionSummaries, filePath, cancellationToken).ConfigureAwait(false);
        }

        await EmitDistributionHtmlReport(notification, outputDirectory, cancellationToken).ConfigureAwait(false);
    }

    // Optional standalone SVG distribution report, mirroring ScaleFish's EmitHtmlReport. Best-effort:
    // a failure here must never fail the run or block the (already-written) markdown/CSV output.
    private async Task EmitDistributionHtmlReport(WriteToMarkDownNotification notification, string outputDirectory, CancellationToken cancellationToken)
    {
        if (!_runSettings.EmitDistributionHtmlReport) return;

        try
        {
            var html = PerformanceDistributionHtmlReportBuilder.Build(notification.ClassExecutionSummaries);
            if (string.IsNullOrEmpty(html)) return;

            var htmlName = DefaultFileSettings.AppendTagsToFilename(
                $"DistributionReport_{_runSettings.TimeStamp:yyyyMMdd-HHmmss}.html", _runSettings.Tags);
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, htmlName), html, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // best-effort: optional report
        }
    }
}