using Sailfish.Contracts.Public.Models;

namespace Tests.Common.Builders;

public class PerformanceRunResultBuilder
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

    public static PerformanceRunResultBuilder Create()
    {
        return new PerformanceRunResultBuilder();
    }

    public PerformanceRunResultBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public PerformanceRunResultBuilder WithMean(double mean)
    {
        _mean = mean;
        return this;
    }

    public PerformanceRunResultBuilder WithStdDev(double stdDev)
    {
        _stdDev = stdDev;
        return this;
    }

    public PerformanceRunResultBuilder WithVariance(double variance)
    {
        _variance = variance;
        return this;
    }

    public PerformanceRunResultBuilder WithMedian(double median)
    {
        _median = median;
        return this;
    }

    public PerformanceRunResultBuilder WithRawExecutionResults(double[] rawExecutionResults)
    {
        _rawExecutionResults = rawExecutionResults;
        return this;
    }

    public PerformanceRunResultBuilder WithSampleSize(int sampleSize)
    {
        _sampleSize = sampleSize;
        return this;
    }

    public PerformanceRunResultBuilder WithNumWarmupIterations(int numWarmupIterations)
    {
        _numWarmupIterations = numWarmupIterations;
        return this;
    }

    public PerformanceRunResultBuilder WithDataWithOutliersRemoved(double[] dataWithOutliersRemoved)
    {
        _dataWithOutliersRemoved = dataWithOutliersRemoved;
        return this;
    }

    public PerformanceRunResultBuilder WithUpperOutliers(double[] upperOutliers)
    {
        _upperOutliers = upperOutliers;
        return this;
    }

    public PerformanceRunResultBuilder WithLowerOutliers(double[] lowerOutliers)
    {
        _lowerOutliers = lowerOutliers;
        return this;
    }

    public PerformanceRunResultBuilder WithTotalNumOutliers(int totalNumOutliers)
    {
        _totalNumOutliers = totalNumOutliers;
        return this;
    }

    public PerformanceRunResult Build()
    {
        // Set default values if properties are null
        return new PerformanceRunResult(
            _displayName ?? "My.Test()",
            _mean ?? 2.0,
            _stdDev ?? 1.0,
            _variance ?? 2.0,
            _median ?? 2.0,
            _rawExecutionResults ?? [],
            _sampleSize ?? 3,
            _numWarmupIterations ?? 2,
            _dataWithOutliersRemoved ?? [],
            _upperOutliers ?? [],
            _lowerOutliers ?? [],
            _totalNumOutliers ?? 0);
    }
}