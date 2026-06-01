using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Analysis.Ai;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;

namespace Sailfish.DefaultHandlers.Ai;

/// <summary>
///     Bridges a completed SailDiff comparison to the Skipper AI layer: builds the grounded context packet, then
///     hands it to the shared <see cref="ISkipperAnalysisRunner" />. Opt-in (<see cref="IRunSettings.RunAiAnalysis" />)
///     and strictly additive — if no real agent is registered the run is entirely unaffected.
/// </summary>
internal sealed class SkipperSailDiffAnalysisHandler : INotificationHandler<SailDiffAnalysisCompleteNotification>
{
    private readonly IPerformanceNarrativeContextBuilder contextBuilder;
    private readonly ISkipperAnalysisRunner runner;
    private readonly IRunSettings runSettings;

    public SkipperSailDiffAnalysisHandler(
        IRunSettings runSettings,
        IPerformanceNarrativeContextBuilder contextBuilder,
        ISkipperAnalysisRunner runner)
    {
        this.runSettings = runSettings;
        this.contextBuilder = contextBuilder;
        this.runner = runner;
    }

    public async Task Handle(SailDiffAnalysisCompleteNotification notification, CancellationToken cancellationToken)
    {
        if (!runSettings.RunAiAnalysis) return;

        var context = contextBuilder.Build(notification, runSettings.SailDiffSettings.Alpha);
        await runner.RunAsync(context, "saildiff", cancellationToken).ConfigureAwait(false);
    }
}
