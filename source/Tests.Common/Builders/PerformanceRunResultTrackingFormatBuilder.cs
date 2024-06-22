using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Tests.Common.Builders;

public class PerformanceRunResultTrackingFormatBuilder
{
    private double[]? dataWithOutliersRemoved;
    private string? displayName;
    private double[]? lowerOutliers;
    private double? mean;
    private double? median;
    private int? numWarmupIterations;
    private double[]? rawExecutionResults;
    private int? sampleSize;
    private double? stdDev;
    private int? totalNumOutliers;
    private double[]? upperOutliers;
    private double? variance;

    public static PerformanceRunResultTrackingFormatBuilder Create()
    {
        return new PerformanceRunResultTrackingFormatBuilder();
    }

    public PerformanceRunResultTrackingFormatBuilder WithDisplayName(string displayName)
    {
        this.displayName = displayName;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithMean(double mean)
    {
        this.mean = mean;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithMedian(double median)
    {
        this.median = median;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithStdDev(double stdDev)
    {
        this.stdDev = stdDev;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithVariance(double variance)
    {
        this.variance = variance;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithRawExecutionResults(double[] rawExecutionResults)
    {
        this.rawExecutionResults = rawExecutionResults;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithSampleSize(int sampleSize)
    {
        this.sampleSize = sampleSize;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithNumWarmupIterations(int numWarmupIterations)
    {
        this.numWarmupIterations = numWarmupIterations;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithDataWithOutliersRemoved(double[] dataWithOutliersRemoved)
    {
        this.dataWithOutliersRemoved = dataWithOutliersRemoved;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithLowerOutliers(double[] lowerOutliers)
    {
        this.lowerOutliers = lowerOutliers;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithUpperOutliers(double[] upperOutliers)
    {
        this.upperOutliers = upperOutliers;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithTotalNumOutliers(int totalNumOutliers)
    {
        this.totalNumOutliers = totalNumOutliers;
        return this;
    }

    public PerformanceRunResultTrackingFormat Build()
    {
        return new PerformanceRunResultTrackingFormat(
            displayName ?? TestCaseIdBuilder.Create().Build().DisplayName,
            mean ?? 5.0,
            median ?? 4.0,
            stdDev ?? 2.0,
            variance ?? 12.0,
            rawExecutionResults ?? [1.0, 2, 4, 7, 8],
            sampleSize ?? 3,
            numWarmupIterations ?? 1,
            dataWithOutliersRemoved ?? [1.0, 2, 4, 7, 8],
            upperOutliers ?? [],
            lowerOutliers ?? [],
            totalNumOutliers ?? 0
        );
    }
}