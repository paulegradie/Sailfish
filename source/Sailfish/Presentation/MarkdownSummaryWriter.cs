using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using Sailfish.Presentation.Markdown;

namespace Sailfish.Presentation;

/// <summary>
///     Writes the run's execution summaries to a markdown file (and the optional distribution HTML report).
///     Previously the handler for the internal <c>WriteToMarkDownNotification</c>; collapsed to a direct
///     service call.
/// </summary>
internal interface IMarkdownSummaryWriter
{
    Task Write(List<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken);
}

internal class MarkdownSummaryWriter : IMarkdownSummaryWriter
{
    private readonly IMarkdownWriter _markdownWriter;
    private readonly IRunSettings _runSettings;

    public MarkdownSummaryWriter(IMarkdownWriter markdownWriter, IRunSettings runSettings)
    {
        _markdownWriter = markdownWriter;
        _runSettings = runSettings;
    }

    public async Task Write(List<IClassExecutionSummary> executionSummaries, CancellationToken cancellationToken)
    {
        var fileName = DefaultFileSettings.AppendTagsToFilename(DefaultFileSettings.DefaultPerformanceResultsFileNameStem(_runSettings.TimeStamp) + ".md", _runSettings.Tags);
        var outputDirectory = _runSettings.LocalOutputDirectory ?? DefaultFileSettings.DefaultOutputDirectory;
        if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

        var filePath = Path.Combine(outputDirectory, fileName);

        // Try to use enhanced formatting if available, otherwise fall back to legacy
        try
        {
            await _markdownWriter.WriteEnhanced(executionSummaries, filePath, cancellationToken).ConfigureAwait(false);
        }
        catch (System.NotImplementedException)
        {
            // Fallback to legacy formatting if enhanced is not implemented
            await _markdownWriter.Write(executionSummaries, filePath, cancellationToken).ConfigureAwait(false);
        }

        await EmitDistributionHtmlReport(executionSummaries, outputDirectory, cancellationToken).ConfigureAwait(false);
    }

    // Optional standalone SVG distribution report, mirroring ScaleFish's EmitHtmlReport. Best-effort:
    // a failure here must never fail the run or block the (already-written) markdown/CSV output.
    private async Task EmitDistributionHtmlReport(List<IClassExecutionSummary> executionSummaries, string outputDirectory, CancellationToken cancellationToken)
    {
        if (!_runSettings.EmitDistributionHtmlReport) return;

        try
        {
            var html = PerformanceDistributionHtmlReportBuilder.Build(executionSummaries);
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
