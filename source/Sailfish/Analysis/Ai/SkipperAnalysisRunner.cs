using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public.Models;
using Sailfish.Logging;
using Sailfish.Presentation.Console;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     The shared Skipper pipeline used by every analysis handler (SailDiff, ScaleFish, ...). Given a grounded
///     context it resolves a review (cache-first, then the agent), then persists and renders it. Artifacts are
///     keyed by <c>analysisKind</c> so analyses that complete in the same run don't overwrite each other.
/// </summary>
internal interface ISkipperAnalysisRunner
{
    Task RunAsync(PerformanceNarrativeContext context, string analysisKind, CancellationToken cancellationToken);
}

internal sealed class SkipperAnalysisRunner : ISkipperAnalysisRunner
{
    private readonly ISailfishAgent agent;
    private readonly IConsoleWriter consoleWriter;
    private readonly ISkipperConsoleFormatter consoleFormatter;
    private readonly ILogger logger;
    private readonly ISkipperResponseCache responseCache;
    private readonly ISkipperReportWriter reportWriter;
    private readonly ISkipperReviewWriter reviewWriter;
    private readonly IRunSettings runSettings;

    public SkipperAnalysisRunner(
        IRunSettings runSettings,
        ISailfishAgent agent,
        ISkipperReviewWriter reviewWriter,
        ISkipperReportWriter reportWriter,
        ISkipperResponseCache responseCache,
        IConsoleWriter consoleWriter,
        ISkipperConsoleFormatter consoleFormatter,
        ILogger logger)
    {
        this.runSettings = runSettings;
        this.agent = agent;
        this.reviewWriter = reviewWriter;
        this.reportWriter = reportWriter;
        this.responseCache = responseCache;
        this.consoleWriter = consoleWriter;
        this.consoleFormatter = consoleFormatter;
        this.logger = logger;
    }

    public async Task RunAsync(PerformanceNarrativeContext context, string analysisKind, CancellationToken cancellationToken)
    {
        if (agent is NoOpSailfishAgent) return; // no real agent registered — stay invisible

        var hasContent = context.Comparisons.Count > 0 || context.Scaling is { Count: > 0 };
        if (!hasContent) return;

        try
        {
            var settings = runSettings.AiAnalysisSettings;

            var review = await ResolveReviewAsync(context, settings, cancellationToken).ConfigureAwait(false);
            if (!review.HasContent) return;

            if (settings.WriteReviewArtifact)
            {
                await reviewWriter.WriteAsync(review, analysisKind, cancellationToken).ConfigureAwait(false);
                await reportWriter.WriteAsync(review, analysisKind, cancellationToken).ConfigureAwait(false);
            }

            if (settings.EmitConsoleSummary)
                consoleWriter.WriteString(consoleFormatter.Format(review));
        }
        catch (Exception ex)
        {
            // Strictly additive: AI analysis must never break or alter a benchmark run.
            logger.Log(LogLevel.Warning, ex, "Skipper AI analysis failed; continuing without it.");
        }
    }

    private async Task<SkipperReview> ResolveReviewAsync(
        PerformanceNarrativeContext context,
        AiAnalysisSettings settings,
        CancellationToken cancellationToken)
    {
        string? cacheKey = null;
        if (settings.UseResponseCache)
        {
            cacheKey = responseCache.ComputeKey(context, settings.Role);
            var cached = await responseCache.TryGetAsync(cacheKey, cancellationToken).ConfigureAwait(false);
            if (cached is not null) return cached;
        }

        var repositoryRoot = ResolveRepositoryRoot();
        var capabilities = new CapabilityRegistry(new ISkipperCapability[] { new CodeReadCapability(repositoryRoot) });
        var session = new SkipperSession(settings.Role, context, capabilities, repositoryRoot);

        var review = await agent.RunAsync(session, cancellationToken).ConfigureAwait(false);

        if (cacheKey is not null && review.HasContent)
            await responseCache.SetAsync(cacheKey, review, cancellationToken).ConfigureAwait(false);

        return review;
    }

    /// <summary>Walks up from the working directory to the nearest Git root; falls back to the working directory.</summary>
    private static string ResolveRepositoryRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git"))) return dir.FullName;
            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
