using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis.Ai;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Logging;
using Sailfish.Presentation.Console;

namespace Sailfish.DefaultHandlers.Ai;

/// <summary>
///     Bridges a completed SailDiff comparison to the AI "Skipper" layer: builds a grounded context packet, runs
///     the registered <see cref="ISailfishAgent" />, then persists and renders the result.
///     <para>
///         The whole path is opt-in (<see cref="IRunSettings.RunAiAnalysis" />), strictly additive, and must never
///         throw into the run. If no real agent is registered, or anything fails, the benchmark output is entirely
///         unaffected.
///     </para>
/// </summary>
internal sealed class SkipperSailDiffAnalysisHandler : INotificationHandler<SailDiffAnalysisCompleteNotification>
{
    private readonly ISailfishAgent agent;
    private readonly IConsoleWriter consoleWriter;
    private readonly IPerformanceNarrativeContextBuilder contextBuilder;
    private readonly ILogger logger;
    private readonly ISkipperResponseCache responseCache;
    private readonly ISkipperReviewWriter reviewWriter;
    private readonly IRunSettings runSettings;

    public SkipperSailDiffAnalysisHandler(
        IRunSettings runSettings,
        ISailfishAgent agent,
        IPerformanceNarrativeContextBuilder contextBuilder,
        ISkipperReviewWriter reviewWriter,
        ISkipperResponseCache responseCache,
        IConsoleWriter consoleWriter,
        ILogger logger)
    {
        this.runSettings = runSettings;
        this.agent = agent;
        this.contextBuilder = contextBuilder;
        this.reviewWriter = reviewWriter;
        this.responseCache = responseCache;
        this.consoleWriter = consoleWriter;
        this.logger = logger;
    }

    public async Task Handle(SailDiffAnalysisCompleteNotification notification, CancellationToken cancellationToken)
    {
        if (!runSettings.RunAiAnalysis) return;     // opt-in
        if (agent is NoOpSailfishAgent) return;     // no real agent registered — stay invisible

        try
        {
            var settings = runSettings.AiAnalysisSettings;

            var context = contextBuilder.Build(notification, runSettings.SailDiffSettings.Alpha);
            if (context.Comparisons.Count == 0) return;

            var review = await ResolveReviewAsync(context, settings, cancellationToken).ConfigureAwait(false);
            if (!review.HasContent) return;

            if (settings.WriteReviewArtifact)
                await reviewWriter.WriteAsync(review, cancellationToken).ConfigureAwait(false);

            if (settings.EmitConsoleSummary && !string.IsNullOrWhiteSpace(review.ConsoleSummary))
                consoleWriter.WriteString(review.ConsoleSummary);
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
