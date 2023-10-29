using System.Text.Json.Serialization;

namespace Sailfish.TestAdapter.TestSettingsParser;

public class SailfishSettings
{
    [JsonPropertyName("DisableOverheadEstimation")]
    public bool DisableOverheadEstimation { get; set; }

    [JsonPropertyName("NumWarmupIterationsOverride")]
    public int? NumWarmupIterationsOverride { get; set; }

    [JsonPropertyName("SampleSizeOverride")]
    public int? SampleSizeOverride { get; set; }
}