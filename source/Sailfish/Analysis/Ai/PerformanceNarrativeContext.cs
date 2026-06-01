using System.Collections.Generic;

namespace Sailfish.Analysis.Ai;

/// <summary>
///     The grounded, authoritative packet handed to an <see cref="ISailfishAgent" />. Every number here is
///     computed by Sailfish / SailDiff — the agent must reason over these figures and never recompute or invent
///     them. Enriched in later phases with an environment snapshot and ScaleFish scaling projections.
/// </summary>
public sealed record PerformanceNarrativeContext(
    IReadOnlyList<SailDiffCaseContext> Comparisons,
    string SailDiffMarkdown,
    EnvironmentSnapshot? Environment);

/// <summary>Grounded before / after figures for one test case, lifted directly from a SailDiff result.</summary>
public sealed record SailDiffCaseContext(
    string DisplayName,
    SkipperVerdict Verdict,
    double MeanBefore,
    double MeanAfter,
    double MedianBefore,
    double MedianAfter,
    double PercentChangeMean,
    double PValue,
    double? AdjustedPValue,
    string ChangeDescription,
    int SampleSizeBefore,
    int SampleSizeAfter,
    bool Failed,
    string? EffectSizeName = null,
    double? EffectSizeValue = null,
    double? MinimumDetectableEffectPercent = null);
