using System.Collections.Generic;
using Sailfish.Mediation;
using Sailfish.Analysis.SailDiff;

namespace Sailfish.Contracts.Public.Notifications;

/// <summary>
///     Published when an in-run method-vs-method comparison group (the one-baseline / N-candidates
///     <c>ComparisonGroup</c> feature) has been analyzed. Carries the authoritative pairwise results so
///     downstream listeners — notably the Skipper AI layer — can reason over the most common comparison users
///     run without recomputing anything. Emitted once per completed comparison group.
/// </summary>
public record MethodComparisonAnalysisCompleteNotification : INotification
{
    public MethodComparisonAnalysisCompleteNotification(
        string GroupName,
        IReadOnlyList<MethodComparisonPairResult> Pairs,
        string ResultsAsMarkdown)
    {
        this.GroupName = GroupName;
        this.Pairs = Pairs;
        this.ResultsAsMarkdown = ResultsAsMarkdown;
    }

    /// <summary>The comparison group's name (the <c>ComparisonGroup</c> label shared by its member methods).</summary>
    public string GroupName { get; init; }

    /// <summary>The pairwise comparison results for the group, primary/baseline (before) vs compared (after).</summary>
    public IReadOnlyList<MethodComparisonPairResult> Pairs { get; init; }

    /// <summary>A concise markdown rendering of the comparison, used to ground the AI narrative.</summary>
    public string ResultsAsMarkdown { get; init; }

    public void Deconstruct(
        out string GroupName,
        out IReadOnlyList<MethodComparisonPairResult> Pairs,
        out string ResultsAsMarkdown)
    {
        GroupName = this.GroupName;
        Pairs = this.Pairs;
        ResultsAsMarkdown = this.ResultsAsMarkdown;
    }
}
