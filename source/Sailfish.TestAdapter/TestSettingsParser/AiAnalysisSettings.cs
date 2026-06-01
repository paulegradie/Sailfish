using System.Text.Json.Serialization;

namespace Sailfish.TestAdapter.TestSettingsParser;

/// <summary>
///     <c>.sailfish.json</c> section that turns the Skipper AI analysis layer on for the VS Test Adapter
///     (<c>dotnet test</c> / Test Explorer) path. Mirrors <c>Sailfish.Analysis.Ai.AiAnalysisSettings</c>.
///     A custom <c>ISailfishAgent</c> must still be registered via <c>IRegisterSailfishServices</c>; without one,
///     enabling this is a no-op (the run is unaffected).
/// </summary>
public class AiAnalysisSettings
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("WriteReviewArtifact")]
    public bool? WriteReviewArtifact { get; set; }

    [JsonPropertyName("EmitConsoleSummary")]
    public bool? EmitConsoleSummary { get; set; }

    [JsonPropertyName("UseResponseCache")]
    public bool? UseResponseCache { get; set; }
}
