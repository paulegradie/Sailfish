using System.Text.Json.Serialization;
using System.Collections.Generic;


namespace Sailfish.Execution;

public interface IExecutionSettings
{
    public bool AsCsv { get; set; }
    public bool AsConsole { get; set; }
    public bool AsMarkdown { get; set; }

    public int NumWarmupIterations { get; set; }
    public int SampleSize { get; set; }
    public bool DisableOverheadEstimation { get; set; }

    // NEW: Adaptive Sampling Configuration
    public bool UseAdaptiveSampling { get; set; }
    public double TargetCoefficientOfVariation { get; set; }
    public int MinimumSampleSize { get; set; }
    public int MaximumSampleSize { get; set; }
    public double ConfidenceLevel { get; set; }
    public IReadOnlyList<double> ReportConfidenceLevels { get; set; }


    // NEW: Enhanced Statistical Configuration
    public double MaxConfidenceIntervalWidth { get; set; }
    public bool UseRelativeConfidenceInterval { get; set; }
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

    // NEW: Adaptive Sampling Properties
    public bool UseAdaptiveSampling { get; set; } = false;
    public double TargetCoefficientOfVariation { get; set; } = 0.05;
    public int MinimumSampleSize { get; set; } = 10;
    public int MaximumSampleSize { get; set; } = 1000;
    public double ConfidenceLevel { get; set; } = 0.95;

    // NEW: Enhanced Statistical Properties
    public IReadOnlyList<double> ReportConfidenceLevels { get; set; } = new List<double> { 0.95, 0.99 };

    public double MaxConfidenceIntervalWidth { get; set; } = 0.20; // 20% relative CI width
    public bool UseRelativeConfidenceInterval { get; set; } = true;
}