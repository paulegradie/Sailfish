using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Public.Serialization.Tracking.V1;

/// <summary>
///     Data structure contract used specifically for serializing and deserializing tracking file data
///     Changes to this constitute a **BREAKING CHANGE** in the Sailfish data persistence contract
///     Do not make changes to this lightly
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

    public bool AsCsv { get; private set; }
    public bool AsConsole { get; private set; }
    public bool AsMarkdown { get; private set; }

    public int NumWarmupIterations { get; private set; }
    public int SampleSize { get; set; }
    public bool DisableOverheadEstimation { get; private set; }
}