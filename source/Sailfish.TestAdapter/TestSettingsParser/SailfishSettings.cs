using System.Text.Json.Serialization;
using Sailfish.Execution;

namespace Sailfish.TestAdapter.TestSettingsParser;

#pragma warning disable CS8618
public class SailfishSettings
{
    public SailDiffSettings SailDiffSettings { get; set; }

    [JsonPropertyName("ResultsDirectory")] public string ResultsDirectory { get; set; }

    [JsonPropertyName("Resolution")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DurationConversion.TimeScaleUnit Resolution { get; set; }

    [JsonPropertyName("Round")] public int Round { get; set; }

    [JsonPropertyName("UseOutlierDetection")]
    public bool UseOutlierDetection { get; set; }

    [JsonPropertyName("DisableOverheadEstimation")]
    public bool DisableOverheadEstimation { get; set; }

    [JsonPropertyName("DisableEverything")]
    public bool DisableEverything { get; set; }
}