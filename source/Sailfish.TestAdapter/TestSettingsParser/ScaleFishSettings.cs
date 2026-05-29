namespace Sailfish.TestAdapter.TestSettingsParser;

/// <summary>
/// JSON-deserialised ScaleFish settings from <c>.sailfish.json</c>. All fields are nullable so the
/// loader can tell "absent" (use library default) from "explicitly set" (override).
/// </summary>
public class ScaleFishSettings
{
    /// <summary>Skip the bootstrap diagnostic entirely when false.</summary>
    public bool? EnableBootstrap { get; set; }

    /// <summary>Number of bootstrap iterations. 0 disables.</summary>
    public int? BootstrapIterations { get; set; }

    /// <summary>Run bootstrap iterations on multiple threads.</summary>
    public bool? EnableParallelBootstrap { get; set; }

    /// <summary>Compute the continuous power-log (b, c) diagnostic.</summary>
    public bool? EnableContinuousExponent { get; set; }

    /// <summary>Δ-AICc threshold for declaring the best model "distinguishable".</summary>
    public double? DistinguishabilityDelta { get; set; }

    /// <summary>Run leave-one-X-out cross-validation as a hold-out check.</summary>
    public bool? EnableCrossValidation { get; set; }

    /// <summary>Fit complexity on per-X percentile data (p50/p95/p99 by default).</summary>
    public bool? EnableTailPercentileFits { get; set; }

    /// <summary>Percentiles to fit when tail-percentile fits are enabled. Values in (0, 1).</summary>
    public double[]? TailPercentiles { get; set; }

    /// <summary>Persist a per-run snapshot to the tracking directory and diff against prior snapshots.</summary>
    public bool? EnableTrendTracking { get; set; }

    /// <summary>Emit a standalone HTML report alongside the markdown one.</summary>
    public bool? EmitHtmlReport { get; set; }
}
