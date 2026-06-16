using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Mediation;
using Sailfish.Analysis.Ai;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;

namespace Sailfish.DefaultHandlers.Ai;

/// <summary>
///     Bridges a completed in-run method-vs-method comparison (the <c>ComparisonGroup</c> feature — one
///     baseline, N candidates — which is the most common comparison users run) to the Skipper AI layer. Builds
///     a grounded comparison context and hands it to the shared <see cref="ISkipperAnalysisRunner" />. Opt-in
///     (<see cref="IRunSettings.RunAiAnalysis" />) and strictly additive — if no real agent is registered the
///     run is entirely unaffected. Artifacts are keyed by group name so several comparison groups in one run do
///     not overwrite each other.
/// </summary>
internal sealed class SkipperMethodComparisonAnalysisHandler : INotificationHandler<MethodComparisonAnalysisCompleteNotification>
{
    private readonly IPerformanceNarrativeContextBuilder contextBuilder;
    private readonly ISkipperAnalysisRunner runner;
    private readonly IRunSettings runSettings;

    public SkipperMethodComparisonAnalysisHandler(
        IRunSettings runSettings,
        IPerformanceNarrativeContextBuilder contextBuilder,
        ISkipperAnalysisRunner runner)
    {
        this.runSettings = runSettings;
        this.contextBuilder = contextBuilder;
        this.runner = runner;
    }

    public async Task Handle(MethodComparisonAnalysisCompleteNotification notification, CancellationToken cancellationToken)
    {
        if (!runSettings.RunAiAnalysis) return;
        if (notification.Pairs.Count == 0) return;

        var context = contextBuilder.BuildComparison(notification);
        await runner.RunAsync(context, AnalysisKindFor(notification.GroupName), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Builds a filesystem-safe artifact key from the comparison group name (e.g. "comparison-MyGroup"),
    ///     so each group's Skipper artifacts are distinct from one another and from the SailDiff/ScaleFish ones.
    /// </summary>
    private static string AnalysisKindFor(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName)) return "comparison";

        var safe = new StringBuilder("comparison-");
        foreach (var c in groupName)
            safe.Append(char.IsLetterOrDigit(c) ? c : '-');
        return safe.ToString();
    }
}
