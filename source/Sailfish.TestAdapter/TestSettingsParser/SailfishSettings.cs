using System.Text.Json.Serialization;

namespace Sailfish.TestAdapter.TestSettingsParser;

#pragma warning disable CS8618
public class SailfishSettings
{
    [JsonPropertyName("_comment")] public string Comment { get; set; }

    public SailfishTestSettings TestSettings { get; set; }
}