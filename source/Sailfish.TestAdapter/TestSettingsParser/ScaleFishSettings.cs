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
}
