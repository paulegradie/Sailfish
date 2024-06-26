using System.Text.Json.Serialization;
using Sailfish.Analysis.SailDiff;

namespace Sailfish.TestAdapter.TestSettingsParser;

public class SailDiffSettings
{
    [JsonPropertyName("TestType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TestType TestType { get; set; }

    [JsonPropertyName("Alpha")]
    public double Alpha { get; set; } = 0.0001;

    [JsonPropertyName("Disabled")]
    public bool Disabled { get; set; }
}