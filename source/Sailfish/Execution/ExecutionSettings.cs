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

internal class ExecutionSettings : IExecutionSettings
{
    public bool AsCsv { get; set; }
    public bool AsConsole { get; set; }
    public bool AsMarkdown { get; set; }

    public int NumWarmupIterations { get; set; }
    public int SampleSize { get; set; }
    public bool DisableOverheadEstimation { get; set; }
}