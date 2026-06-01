using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis.Ai;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;

namespace Sailfish.DefaultHandlers.Ai;

/// <summary>
///     Bridges a completed ScaleFish complexity analysis to the Skipper AI layer. Builds a scaling context packet
///     (best-fit Big-O, goodness of fit, distinguishability, and projections to larger N) and hands it to the
///     shared <see cref="ISkipperAnalysisRunner" /> — answering "how does this scale, and what happens at 10× the
///     data?". Opt-in and strictly additive.
/// </summary>
internal sealed class SkipperScaleFishAnalysisHandler : INotificationHandler<ScaleFishAnalysisCompleteNotification>
{
    private readonly IPerformanceNarrativeContextBuilder contextBuilder;
    private readonly ISkipperAnalysisRunner runner;
    private readonly IRunSettings runSettings;

    public SkipperScaleFishAnalysisHandler(
        IRunSettings runSettings,
        IPerformanceNarrativeContextBuilder contextBuilder,
        ISkipperAnalysisRunner runner)
    {
        this.runSettings = runSettings;
        this.contextBuilder = contextBuilder;
        this.runner = runner;
    }

    public async Task Handle(ScaleFishAnalysisCompleteNotification notification, CancellationToken cancellationToken)
    {
        if (!runSettings.RunAiAnalysis) return;

        var context = contextBuilder.BuildScaling(notification);
        await runner.RunAsync(context, "scalefish", cancellationToken).ConfigureAwait(false);
    }
}
