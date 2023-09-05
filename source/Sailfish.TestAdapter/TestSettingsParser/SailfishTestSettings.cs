using System.Text.Json.Serialization;
using Sailfish.Analysis.SailDiff;
using Sailfish.Execution;

#pragma warning disable CS8618
namespace Sailfish.TestAdapter.TestSettingsParser;

public class SailfishTestSettings
{
    [JsonPropertyName("ResultsDirectory")] public string ResultsDirectory { get; set; }


    [JsonPropertyName("TestType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TestType TestType { get; set; }

    [JsonPropertyName("Resolution")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DurationConversion.TimeScaleUnit Resolution { get; set; }


    [JsonPropertyName("Alpha")] public double Alpha { get; set; }

    [JsonPropertyName("Round")] public int Round { get; set; }

    [JsonPropertyName("UseOutlierDetection")] public bool UseOutlierDetection { get; set; }
    [JsonPropertyName("Disabled")] public bool Disabled { get; set; }

    [JsonPropertyName("DisableOverheadEstimation")]
    public bool DisableOverheadEstimation { get; set; }
    
    [JsonPropertyName("DisableEverything")]
    public bool DisableEverything { get; set; }
    
    
}