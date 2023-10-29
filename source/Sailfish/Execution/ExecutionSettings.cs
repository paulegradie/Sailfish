using System.Text.Json.Serialization;

namespace Sailfish.Execution;

public interface IExecutionSettings
{
    public bool AsCsv { get; set; }
    public bool AsConsole { get; set; }
    public bool AsMarkdown { get; set; }

    public int NumWarmupIterations { get; set; }
    public int SampleSize { get; set; }
    public bool DisableOverheadEstimation { get; set; }
}

public class ExecutionSettings : IExecutionSettings
{
    [JsonConstructor]
    public ExecutionSettings()
    {
    }

    public ExecutionSettings(bool asCsv, bool asConsole, bool asMarkdown, int sampleSize, int numWarmupIterations)
    {
        AsCsv = asCsv;
        AsConsole = asConsole;
        AsMarkdown = asMarkdown;
        SampleSize = sampleSize;
        NumWarmupIterations = numWarmupIterations;
    }

    public bool AsCsv { get; set; }
    public bool AsConsole { get; set; }
    public bool AsMarkdown { get; set; }

    public int NumWarmupIterations { get; set; }
    public int SampleSize { get; set; }
    public bool DisableOverheadEstimation { get; set; }
}