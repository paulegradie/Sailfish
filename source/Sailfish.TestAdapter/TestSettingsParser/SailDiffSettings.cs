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

    // Opt-in TOST equivalence margin, in percent (e.g. 5 ⇒ ±5%). Null leaves equivalence testing off.
    [JsonPropertyName("EquivalenceMarginPercent")]
    public double? EquivalenceMarginPercent { get; set; }

    // Accepted here as a convenience too — the canonical home is GlobalSettings.DistributionPlotStyle.
    // "Histogram" (default) or "BoxPlot".
    [JsonPropertyName("DistributionPlotStyle")]
    public string? DistributionPlotStyle { get; set; }
}