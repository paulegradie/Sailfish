using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Serialization.V1;

public class ExecutionSettingsTrackingFormat
{
    [JsonConstructor]
    public ExecutionSettingsTrackingFormat()
    {
    }

    public ExecutionSettingsTrackingFormat(bool asCsv, bool asConsole, bool asMarkdown, int numWarmupIterations, int numIterations, bool disableOverheadEstimation)
    {
        AsCsv = asCsv;
        AsConsole = asConsole;
        AsMarkdown = asMarkdown;
        NumWarmupIterations = numWarmupIterations;
        NumIterations = numIterations;
        DisableOverheadEstimation = disableOverheadEstimation;
    }

    public bool AsCsv { get; set; }
    public bool AsConsole { get; set; }
    public bool AsMarkdown { get; set; }

    public int NumWarmupIterations { get; set; }
    public int NumIterations { get; set; }
    public bool DisableOverheadEstimation { get; set; }
}