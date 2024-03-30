using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis;
using Sailfish.Execution;

namespace Sailfish.Contracts.Public.Models;

public class PerformanceRunResult(string displayName, double mean, double stdDev, double variance,
    double median, double[] rawExecutionResults, int sampleSize, int numWarmupIterations,
    double[] dataWithOutliersRemoved, double[] upperOutliers, double[] lowerOutliers,
    int totalNumOutliers)
{
    public string DisplayName { get; } = displayName;
    public double Mean { get; } = mean;
    public double Median { get; } = median;
    public double StdDev { get; } = stdDev;
    public double Variance { get; } = variance;

    public double[] RawExecutionResults { get; } = rawExecutionResults;

    public int SampleSize { get; } = sampleSize;
    public int NumWarmupIterations { get; } = numWarmupIterations;

    public double[] DataWithOutliersRemoved { get; } = dataWithOutliersRemoved;
    public double[] LowerOutliers { get; } = lowerOutliers;
    public double[] UpperOutliers { get; } = upperOutliers;
    public int TotalNumOutliers { get; } = totalNumOutliers;

    public static PerformanceRunResult ConvertFromPerfTimer(TestCaseId testCaseId, PerformanceTimer performanceTimer, IExecutionSettings executionSettings)
    {
        var executionIterations = performanceTimer.ExecutionIterationPerformances
            .Select(x => x.GetDurationFromTicks())
            .Select(x => x.MilliSeconds.Duration)
            .ToArray();
        return ConvertWithOutlierAnalysis(testCaseId, executionSettings, executionIterations);
    }

    private static PerformanceRunResult ConvertWithOutlierAnalysis(
        TestCaseId testCaseId,
        IExecutionSettings executionSettings,
        IReadOnlyList<double> executionIterations)
    {
        var detector = new SailfishOutlierDetector();

        var (rawExecutionResults, cleanData, lowerOutliers, upperOutliers, totalNumOutliers) = detector.DetectOutliers(executionIterations);

        var mean = cleanData.Mean();
        var median = cleanData.Median();
        var stdDev = executionIterations.Count > 1 ? cleanData.StandardDeviation() : 0;
        var variance = executionIterations.Count > 1 ? cleanData.Variance() : 0;
        return new PerformanceRunResult(testCaseId.DisplayName,
            mean, stdDev, variance, median, rawExecutionResults,
            executionSettings.SampleSize, executionSettings.NumWarmupIterations, cleanData,
            upperOutliers.ToArray(), lowerOutliers.ToArray(), totalNumOutliers);
    }
}