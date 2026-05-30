using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff.Statistics;

/// <summary>
/// Single source of truth for significance decisions across SailDiff.
/// </summary>
/// <remarks>
/// <para>
/// SailDiff has multiple output paths (per-test markdown, N×N matrix, CSV, IDE banner) and
/// they used to apply hardcoded 0.05 thresholds even when the user had configured a different
/// <see cref="SailDiffSettings.Alpha"/>. This helper centralises the cutoff so a single
/// configured α drives both the per-pair test decisions <em>and</em> the BH-FDR–adjusted
/// q-value cell labels in the matrix.
/// </para>
/// <para>
/// When a formatter does not have direct access to the originating settings, it can read the
/// alpha from a per-message metadata key written by the upstream comparison processor — use
/// <see cref="MetadataKey"/> and <see cref="ReadFromMetadata"/>.
/// </para>
/// </remarks>
public static class SailDiffSignificance
{
    /// <summary>
    /// Metadata key under which the alpha used for a comparison is stored alongside
    /// PairwisePValues. Formatters that cannot inject settings should read this key.
    /// </summary>
    public const string MetadataKey = "SailDiffAlpha";

    /// <summary>
    /// Fallback alpha used when no value has been supplied or is reachable.
    /// Matches <see cref="SailDiffSettings"/>'s default.
    /// </summary>
    public const double FallbackAlpha = 0.05;

    /// <summary>True when <paramref name="pOrQ"/> is non-negative and ≤ <paramref name="alpha"/>.</summary>
    public static bool IsSignificant(double pOrQ, double alpha) => pOrQ >= 0 && pOrQ <= alpha;

    /// <summary>
    /// Same as <see cref="IsSignificant(double, double)"/> but additionally requires the value
    /// to be strictly positive — useful for q-value cell labels where 0 means "no q computed".
    /// </summary>
    public static bool IsSignificantPositive(double pOrQ, double alpha) => pOrQ > 0 && pOrQ <= alpha;

    /// <summary>
    /// Read the alpha stored under <see cref="MetadataKey"/> in a metadata dictionary, falling
    /// back to <see cref="FallbackAlpha"/> when absent or malformed.
    /// </summary>
    public static double ReadFromMetadata(IReadOnlyDictionary<string, object>? metadata)
    {
        if (metadata is null) return FallbackAlpha;
        if (!metadata.TryGetValue(MetadataKey, out var raw)) return FallbackAlpha;
        return raw switch
        {
            double d when d > 0 && d < 1 => d,
            float f when f > 0 && f < 1 => f,
            _ => FallbackAlpha
        };
    }
}
