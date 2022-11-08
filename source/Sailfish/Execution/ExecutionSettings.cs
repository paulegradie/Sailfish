namespace Sailfish.Execution;

internal class ExecutionSettings
{
    public bool AsCsv { get; set; }
    public bool AsConsole { get; set; }
    public bool AsMarkdown { get; set; }

    public int NumWarmupIterations { get; set; }
    public int NumIterations { get; set; }
}