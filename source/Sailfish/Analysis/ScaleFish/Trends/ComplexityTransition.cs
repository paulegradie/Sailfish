namespace Sailfish.Analysis.ScaleFish.Trends;

/// <summary>
/// Categorises how a ScaleFish classification changed between two history snapshots for the same key.
/// </summary>
public enum ComplexityTransitionKind
{
    /// <summary>Best family unchanged and parameters within tolerance.</summary>
    Stable,

    /// <summary>Best family is the same but the fitted scale changed materially.</summary>
    ParameterDrift,

    /// <summary>Best family changed (e.g. Linear → Quadratic). The headline regression case.</summary>
    ClassChanged,

    /// <summary>
    /// Best family is the same and parameters stable, but the result moved across the distinguishability
    /// threshold (e.g. was distinguishable, no longer is, or vice versa).
    /// </summary>
    DistinguishabilityChanged
}

/// <summary>
/// Describes a single difference between an older and newer history snapshot for the same key.
/// </summary>
public class ComplexityTransition
{
    public ComplexityTransition(
        string key,
        ComplexityTransitionKind kind,
        ComplexityHistoryEntry previous,
        ComplexityHistoryEntry current,
        string summary)
    {
        Key = key;
        Kind = kind;
        Previous = previous;
        Current = current;
        Summary = summary;
    }

    public string Key { get; init; }
    public ComplexityTransitionKind Kind { get; init; }
    public ComplexityHistoryEntry Previous { get; init; }
    public ComplexityHistoryEntry Current { get; init; }
    public string Summary { get; init; }

    /// <summary>True when this transition is worth flagging in CI (anything but Stable).</summary>
    public bool IsRegression => Kind != ComplexityTransitionKind.Stable;
}
