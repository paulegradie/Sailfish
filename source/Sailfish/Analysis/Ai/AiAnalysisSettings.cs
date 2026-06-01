namespace Sailfish.Analysis.Ai;

/// <summary>
///     Settings for the Skipper AI analysis layer. Mirrors the shape of <c>SailDiffSettings</c> /
///     <c>ScaleFishSettings</c>; supply a configured instance via
///     <c>RunSettingsBuilder.WithAiAnalysis(AiAnalysisSettings)</c>.
/// </summary>
public sealed class AiAnalysisSettings
{
    public AiAnalysisSettings(
        SkipperRole role = SkipperRole.Explain,
        bool writeReviewArtifact = true,
        bool emitConsoleSummary = true,
        bool useResponseCache = true)
    {
        Role = role;
        WriteReviewArtifact = writeReviewArtifact;
        EmitConsoleSummary = emitConsoleSummary;
        UseResponseCache = useResponseCache;
    }

    /// <summary>The authority the agent runs under. Phase 0 supports <see cref="SkipperRole.Explain" />.</summary>
    public SkipperRole Role { get; }

    /// <summary>When true, the <see cref="SkipperReview" /> is serialized to <c>skipper-review_*.json</c> beside the run output.</summary>
    public bool WriteReviewArtifact { get; }

    /// <summary>When true, the review's short narrative is printed to the console beneath the SailDiff table.</summary>
    public bool EmitConsoleSummary { get; }

    /// <summary>When true, identical context packets reuse a cached review (no re-spend, and stable output).</summary>
    public bool UseResponseCache { get; }
}
