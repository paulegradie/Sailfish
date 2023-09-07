using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Serialization.V1;

/// <summary>
/// Data structure contract used specifically for serializing and deserializing tracking file data
/// Changes to this constitute a **BREAKING CHANGE** in the Sailfish data persistence contract
/// Do not make changes to this lightly
/// </summary>
public class ExecutionSettingsTrackingFormat
{
    [JsonConstructor]
    public ExecutionSettingsTrackingFormat()
    {
    }

    public ExecutionSettingsTrackingFormat(bool asCsv, bool asConsole, bool asMarkdown, int numWarmupIterations, int sampleSize, bool disableOverheadEstimation)
    {
        AsCsv = asCsv;
        AsConsole = asConsole;
        AsMarkdown = asMarkdown;
        NumWarmupIterations = numWarmupIterations;
        SampleSize = sampleSize;
        DisableOverheadEstimation = disableOverheadEstimation;
    }

    public bool AsCsv { get; set; }
    public bool AsConsole { get; set; }
    public bool AsMarkdown { get; set; }

    public int NumWarmupIterations { get; set; }
    public int SampleSize { get; set; }
    public bool DisableOverheadEstimation { get; set; }
}