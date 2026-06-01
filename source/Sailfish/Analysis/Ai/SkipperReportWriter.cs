using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Models;
using Sailfish.Presentation;

namespace Sailfish.Analysis.Ai;

internal interface ISkipperReportWriter
{
    Task WriteAsync(SkipperReview review, string analysisKind, CancellationToken cancellationToken);
}

/// <summary>
///     Persists the agent's full causal write-up as a human-readable <c>skipper-report_*.md</c> beside the run
///     output. This is the deep artifact (call paths, cited code, suggested fixes) that complements the terse
///     console block; <c>review.json</c> remains the machine-readable sibling.
/// </summary>
internal sealed class SkipperReportWriter : ISkipperReportWriter
{
    private readonly IRunSettings runSettings;

    public SkipperReportWriter(IRunSettings runSettings)
    {
        this.runSettings = runSettings;
    }

    public async Task WriteAsync(SkipperReview review, string analysisKind, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(review.MarkdownReport)) return;

        var directory = runSettings.LocalOutputDirectory;
        Directory.CreateDirectory(directory);

        var fileName = DefaultFileSettings.AppendTagsToFilename(
            $"skipper-report_{runSettings.TimeStamp:yyyyMMdd_HHmmss}_{analysisKind}.md",
            runSettings.Tags);
        var filePath = Path.Join(directory, fileName);

        await File.WriteAllTextAsync(filePath, review.MarkdownReport, cancellationToken).ConfigureAwait(false);
    }
}
