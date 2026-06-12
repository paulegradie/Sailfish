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

    // Explicit 'before' tracking file(s) to compare this run against. Sailfish does NOT auto-compare against
    // the previous run; a historical comparison happens only when you name the before file here (absolute, or
    // relative to the working directory). Leave null/empty for no historical comparison.
    [JsonPropertyName("ProvidedBeforeTrackingFiles")]
    public string[]? ProvidedBeforeTrackingFiles { get; set; }
}