using System.Text.Json.Serialization;

namespace Sailfish.TestAdapter.TestSettingsParser;

/// <summary>
/// JSON-deserialised Trawl (load testing) settings from <c>.sailfish.json</c>. All fields are nullable so
/// the loader can tell "absent" (use the library default / per-scenario attribute value) from "explicitly
/// set" (override every scenario).
/// </summary>
public class TrawlSettings
{
    /// <summary>Global kill switch for <c>[Trawl]</c> load scenarios.</summary>
    [JsonPropertyName("Disabled")]
    public bool? Disabled { get; set; }

    /// <summary>Override the number of virtual users for every closed-model scenario.</summary>
    [JsonPropertyName("VirtualUsersOverride")]
    public int? VirtualUsersOverride { get; set; }

    /// <summary>Cap the sustained (measured) load duration in seconds for every scenario.</summary>
    [JsonPropertyName("MaxDurationSecondsOverride")]
    public double? MaxDurationSecondsOverride { get; set; }

    /// <summary>Override the warmup duration in seconds for every scenario.</summary>
    [JsonPropertyName("WarmupSecondsOverride")]
    public double? WarmupSecondsOverride { get; set; }

    /// <summary>Fail a scenario's test case when it regresses significantly vs its prior run (CI gate).</summary>
    [JsonPropertyName("FailOnRegression")]
    public bool? FailOnRegression { get; set; }
}
