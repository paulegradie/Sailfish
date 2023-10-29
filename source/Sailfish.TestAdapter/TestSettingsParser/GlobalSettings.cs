using System.Text.Json.Serialization;

namespace Sailfish.TestAdapter.TestSettingsParser;

#pragma warning disable CS8618
public class GlobalSettings
{
    [JsonPropertyName("Round")] public int Round { get; set; }

    [JsonPropertyName("UseOutlierDetection")]
    public bool UseOutlierDetection { get; set; }

    [JsonPropertyName("ResultsDirectory")] public string ResultsDirectory { get; set; } = string.Empty;

    [JsonPropertyName("DisableEverything")]
    public bool DisableEverything { get; set; }
}