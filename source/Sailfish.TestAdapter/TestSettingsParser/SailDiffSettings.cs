using System.Text.Json.Serialization;
using Sailfish.Analysis.SailDiff;

#pragma warning disable CS8618
namespace Sailfish.TestAdapter.TestSettingsParser;

public class SailDiffSettings
{
    [JsonPropertyName("TestType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TestType TestType { get; set; }

    [JsonPropertyName("Alpha")] public double Alpha { get; set; }

    [JsonPropertyName("Disabled")] public bool Disabled { get; set; }
}