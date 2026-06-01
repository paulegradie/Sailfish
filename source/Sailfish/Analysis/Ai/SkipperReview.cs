using System;
using System.Collections.Generic;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     How Skipper classifies a comparison. Mirrors SailDiff's vocabulary: a comparison is "not significant"
///     (never "no change"), a regression is <em>slower</em>, and an improvement is <em>faster</em>.
/// </summary>
public enum SkipperVerdict
{
    Improved,
    Regressed,
    NotSignificant,
    Inconclusive
}

/// <summary>A single diagnosis Skipper produced for a test case, together with the code it actually cited.</summary>
public sealed record Finding(
    string TestCaseDisplayName,
    SkipperVerdict Verdict,
    string Summary,
    IReadOnlyList<string> CitedSourceLocations,
    double Confidence);

/// <summary>
///     The kind of action Skipper proposes. The local Explain path never emits any; these are reserved for the
///     action-taking future, where a host-supplied <see cref="IActionExecutor" /> decides what (if anything) runs.
/// </summary>
public enum ProposedActionKind
{
    FileTicket,
    OpenPullRequest,
    RunTelemetryQuery,
    CreateBenchmark,
    Custom
}

/// <summary>An action Skipper <b>proposes</b>. Skipper never executes actions itself — see <see cref="IActionExecutor" />.</summary>
public sealed record ProposedAction(
    ProposedActionKind Kind,
    string Title,
    string Detail,
    IReadOnlyDictionary<string, string> Parameters);

/// <summary>
///     The structured result of an analysis. <see cref="Findings" /> are diagnoses (the local Explain path stops
///     here); <see cref="Actions" /> are proposals only. Serialized to <c>skipper-review_*.json</c> so a decoupled
///     orchestrator can consume it and act under its own approval policy.
/// </summary>
public sealed record SkipperReview(
    SkipperVerdict OverallVerdict,
    IReadOnlyList<Finding> Findings,
    IReadOnlyList<ProposedAction> Actions,
    string ConsoleSummary,
    string MarkdownReport)
{
    /// <summary>
    ///     An empty review. The no-op default agent returns this, and any agent may return it to signal
    ///     "nothing worth saying" — in which case nothing is rendered or persisted.
    /// </summary>
    public static SkipperReview Empty { get; } = new(
        SkipperVerdict.Inconclusive,
        Array.Empty<Finding>(),
        Array.Empty<ProposedAction>(),
        string.Empty,
        string.Empty);

    /// <summary>True when the review carries anything worth rendering or persisting.</summary>
    public bool HasContent =>
        Findings is { Count: > 0 } ||
        Actions is { Count: > 0 } ||
        !string.IsNullOrWhiteSpace(ConsoleSummary) ||
        !string.IsNullOrWhiteSpace(MarkdownReport);
}
