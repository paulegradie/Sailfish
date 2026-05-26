namespace Sailfish;

/// <summary>
/// Pre-tuned measurement policies applied via <see cref="RunSettingsBuilder.WithPreset"/>.
/// Presets seed adaptive sampling, outlier handling, and SailDiff defaults; any subsequent
/// explicit <c>WithX</c> call on the builder overrides the preset value for that field.
/// </summary>
public enum SailfishPreset
{
    /// <summary>
    /// Sensible defaults for local development and most CI runs. Matches Sailfish's built-in attribute defaults.
    /// CV 0.05, CI 0.20, MinN 10, MaxN 1000, OutlierStrategy.RemoveUpper, SailDiff alpha 0.001.
    /// </summary>
    Default,

    /// <summary>
    /// Tighter convergence for release gates and baseline verification where small regressions matter.
    /// CV 0.03, CI 0.12, MinN 50, MaxN 2000, OutlierStrategy.RemoveUpper, SailDiff alpha 0.0005.
    /// </summary>
    Tight,

    /// <summary>
    /// Looser convergence for shared or noisy CI hosts where predictable completion time matters more than micro-changes.
    /// CV 0.10, CI 0.30, MinN 10, MaxN 1000, OutlierStrategy.Adaptive, SailDiff alpha 0.01.
    /// </summary>
    Relaxed
}
