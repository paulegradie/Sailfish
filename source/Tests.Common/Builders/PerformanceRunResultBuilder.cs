using Sailfish.Contracts.Public.Models;

namespace Tests.Common.Builders;

public class PerformanceRunResultBuilder
{
    private string? displayName;
    private double? mean;
    private double? stdDev;
    private double? variance;
    private double? median;
    private double[]? rawExecutionResults;
    private int? sampleSize;
    private int? numWarmupIterations;
    private double[]? dataWithOutliersRemoved;
    private double[]? upperOutliers;
    private double[]? lowerOutliers;
    private int? totalNumOutliers;

    public static PerformanceRunResultBuilder Create() => new();

    public PerformanceRunResultBuilder WithDisplayName(string displayName)
    {
        this.displayName = displayName;
        return this;
    }

    public PerformanceRunResultBuilder WithMean(double mean)
    {
        this.mean = mean;
        return this;
    }

    public PerformanceRunResultBuilder WithStdDev(double stdDev)
    {
        this.stdDev = stdDev;
        return this;
    }

    public PerformanceRunResultBuilder WithVariance(double variance)
    {
        this.variance = variance;
        return this;
    }

    public PerformanceRunResultBuilder WithMedian(double median)
    {
        this.median = median;
        return this;
    }

    public PerformanceRunResultBuilder WithRawExecutionResults(double[] rawExecutionResults)
    {
        this.rawExecutionResults = rawExecutionResults;
        return this;
    }

    public PerformanceRunResultBuilder WithSampleSize(int sampleSize)
    {
        this.sampleSize = sampleSize;
        return this;
    }

    public PerformanceRunResultBuilder WithNumWarmupIterations(int numWarmupIterations)
    {
        this.numWarmupIterations = numWarmupIterations;
        return this;
    }

    public PerformanceRunResultBuilder WithDataWithOutliersRemoved(double[] dataWithOutliersRemoved)
    {
        this.dataWithOutliersRemoved = dataWithOutliersRemoved;
        return this;
    }

    public PerformanceRunResultBuilder WithUpperOutliers(double[] upperOutliers)
    {
        this.upperOutliers = upperOutliers;
        return this;
    }

    public PerformanceRunResultBuilder WithLowerOutliers(double[] lowerOutliers)
    {
        this.lowerOutliers = lowerOutliers;
        return this;
    }

    public PerformanceRunResultBuilder WithTotalNumOutliers(int totalNumOutliers)
    {
        this.totalNumOutliers = totalNumOutliers;
        return this;
    }

    public PerformanceRunResult Build()
    {
        // Set default values if properties are null
        return new PerformanceRunResult(
            displayName ?? "My.Test()",
            mean ?? 2.0,
            stdDev ?? 1.0,
            variance ?? 2.0,
            median ?? 2.0,
            rawExecutionResults ?? [],
            sampleSize ?? 3,
            numWarmupIterations ?? 2,
            dataWithOutliersRemoved ?? [],
            upperOutliers ?? [],
            lowerOutliers ?? [],
            totalNumOutliers ?? 0);
    }
}