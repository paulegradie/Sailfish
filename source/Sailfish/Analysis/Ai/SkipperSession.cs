namespace Sailfish.Analysis.Ai;

/// <summary>
///     The authority an <see cref="ISailfishAgent" /> operates under for a single session. Phase 0 only ever
///     issues <see cref="Explain" />; the remaining roles reserve the same pipeline for future automation
///     (CI review, remediation via PR / ticket, and benchmark authoring) without a contract change.
/// </summary>
public enum SkipperRole
{
    /// <summary>Read-only: explain what changed and why. The only role used locally today.</summary>
    Explain,

    /// <summary>Produce a pass / warn / fail verdict suitable for gating a pipeline (future).</summary>
    Review,

    /// <summary>Propose remediations such as opening a PR or filing a ticket (future).</summary>
    Remediate,

    /// <summary>Synthesize a targeted benchmark for a diff and run it before / after (future).</summary>
    Author
}

/// <summary>
///     Everything an <see cref="ISailfishAgent" /> needs for one analysis: the role (authority), the grounded
///     <see cref="PerformanceNarrativeContext" />, the set of granted <see cref="Capabilities" />, and the
///     repository root the agent may read from when granted <see cref="ICodeReadCapability" />.
/// </summary>
public sealed record SkipperSession(
    SkipperRole Role,
    PerformanceNarrativeContext Context,
    ICapabilityRegistry Capabilities,
    string RepositoryRoot);
