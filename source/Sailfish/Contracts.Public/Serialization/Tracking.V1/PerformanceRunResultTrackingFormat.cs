using System.Text.Json.Serialization;

#pragma warning disable CS8618


namespace Sailfish.Contracts.Public.Serialization.Tracking.V1;

/// <summary>
/// Data structure contract used specifically for serializing and deserializing tracking file data
/// Changes to this constitute a **BREAKING CHANGE** in the Sailfish data persistence contract
/// Do not make changes to this lightly
/// </summary>
public class PerformanceRunResultTrackingFormat
{
    [JsonConstructor]
    public PerformanceRunResultTrackingFormat()
    {
    }

    public PerformanceRunResultTrackingFormat(string displayName, double mean, double median,
        double stdDev, double variance, double[] rawExecutionResults, int sampleSize, int numWarmupIterations, double[] dataWithOutliersRemoved, double[] upperOutliers,
        double[] lowerOutliers, int totalNumOutliers)
    {
        DisplayName = displayName;
        Mean = mean;
        Median = median;
        StdDev = stdDev;
        Variance = variance;
        RawExecutionResults = rawExecutionResults;
        SampleSize = sampleSize;
        NumWarmupIterations = numWarmupIterations;
        DataWithOutliersRemoved = dataWithOutliersRemoved;
        LowerOutliers = lowerOutliers;
        UpperOutliers = upperOutliers;
        TotalNumOutliers = totalNumOutliers;
    }

    public string DisplayName { get; init; }
    public double Mean { get; init; }
    public double Median { get; init; }
    public double StdDev { get; init; }
    public double Variance { get; init; }

    public double[] RawExecutionResults { get; init; } // milliseconds

    public int SampleSize { get; set; }
    public int NumWarmupIterations { get; set; }

    public double[] DataWithOutliersRemoved { get; init; } // milliseconds
    public double[] LowerOutliers { get; init; }
    public double[] UpperOutliers { get; init; }
    public int TotalNumOutliers { get; init; }
}