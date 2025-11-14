using Sailfish.Contracts.Public.Serialization.Tracking.V1;

namespace Tests.Common.Builders;

public class PerformanceRunResultTrackingFormatBuilder
{
    private double[]? _dataWithOutliersRemoved;
    private string? _displayName;
    private double[]? _lowerOutliers;
    private double? _mean;
    private double? _median;
    private int? _numWarmupIterations;
    private double[]? _rawExecutionResults;
    private int? _sampleSize;
    private double? _stdDev;
    private int? _totalNumOutliers;
    private double[]? _upperOutliers;
    private double? _variance;

    public static PerformanceRunResultTrackingFormatBuilder Create()
    {
        return new PerformanceRunResultTrackingFormatBuilder();
    }

    public PerformanceRunResultTrackingFormatBuilder WithDisplayName(string displayName)
    {
        this._displayName = displayName;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithMean(double mean)
    {
        this._mean = mean;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithMedian(double median)
    {
        this._median = median;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithStdDev(double stdDev)
    {
        this._stdDev = stdDev;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithVariance(double variance)
    {
        this._variance = variance;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithRawExecutionResults(double[] rawExecutionResults)
    {
        this._rawExecutionResults = rawExecutionResults;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithSampleSize(int sampleSize)
    {
        this._sampleSize = sampleSize;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithNumWarmupIterations(int numWarmupIterations)
    {
        this._numWarmupIterations = numWarmupIterations;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithDataWithOutliersRemoved(double[] dataWithOutliersRemoved)
    {
        this._dataWithOutliersRemoved = dataWithOutliersRemoved;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithLowerOutliers(double[] lowerOutliers)
    {
        this._lowerOutliers = lowerOutliers;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithUpperOutliers(double[] upperOutliers)
    {
        this._upperOutliers = upperOutliers;
        return this;
    }

    public PerformanceRunResultTrackingFormatBuilder WithTotalNumOutliers(int totalNumOutliers)
    {
        this._totalNumOutliers = totalNumOutliers;
        return this;
    }

    public PerformanceRunResultTrackingFormat Build()
    {
        return new PerformanceRunResultTrackingFormat(
            _displayName ?? TestCaseIdBuilder.Create().Build().DisplayName,
            _mean ?? 5.0,
            _median ?? 4.0,
            _stdDev ?? 2.0,
            _variance ?? 12.0,
            _rawExecutionResults ?? [1.0, 2, 4, 7, 8],
            _sampleSize ?? 3,
            _numWarmupIterations ?? 1,
            _dataWithOutliersRemoved ?? [1.0, 2, 4, 7, 8],
            _upperOutliers ?? [],
            _lowerOutliers ?? [],
            _totalNumOutliers ?? 0
        );
    }
}