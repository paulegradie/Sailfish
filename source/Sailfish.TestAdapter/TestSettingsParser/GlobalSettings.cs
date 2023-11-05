using System.Text.Json.Serialization;

namespace Sailfish.TestAdapter.TestSettingsParser;

#pragma warning disable CS8618
public class GlobalSettings
{
    [JsonPropertyName("Round")] public int Round { get; set; }

    [JsonPropertyName("DisableOutlierDetection")]
    public bool DisableOutlierDetection { get; set; }

    [JsonPropertyName("ResultsDirectory")] public string ResultsDirectory { get; set; } = string.Empty;

    [JsonPropertyName("DisableEverything")]
    public bool DisableEverything { get; set; }
}