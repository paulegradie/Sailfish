namespace Sailfish.Analysis;

/// <summary>
/// Strategies for handling statistical outliers detected in a sample set.
/// </summary>
public enum OutlierStrategy
{
    /// <summary>Remove values above the upper fence only.</summary>
    RemoveUpper,
    /// <summary>Remove values below the lower fence only.</summary>
    RemoveLower,
    /// <summary>Remove values below the lower fence and above the upper fence.</summary>
    RemoveAll,
    /// <summary>Do not remove any outliers (report only).</summary>
    DontRemove,
    /// <summary>Choose the side(s) to remove based on the data asymmetry.</summary>
    Adaptive
}

