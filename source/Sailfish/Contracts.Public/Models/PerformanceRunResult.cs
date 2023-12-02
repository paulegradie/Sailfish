using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis;
using Sailfish.Execution;

namespace Sailfish.Contracts.Public.Models;

public class PerformanceRunResult
{
    public PerformanceRunResult(string displayName, double mean, double stdDev, double variance,
        double median, double[] rawExecutionResults, int sampleSize, int numWarmupIterations, 
        double[] dataWithOutliersRemoved, double[] upperOutliers, double[] lowerOutliers,
        int totalNumOutliers)
    {
        DisplayName = displayName;
        Mean = mean;
        StdDev = stdDev;
        Variance = variance;
        Median = median;
        RawExecutionResults = rawExecutionResults;
        SampleSize = sampleSize;
        NumWarmupIterations = numWarmupIterations;
        DataWithOutliersRemoved = dataWithOutliersRemoved;
        UpperOutliers = upperOutliers;
        LowerOutliers = lowerOutliers;
        TotalNumOutliers = totalNumOutliers;
    }

    private const double Tolerance = 0.000000001;
    public string DisplayName { get; }
    public double Mean { get; }
    public double Median { get; }
    public double StdDev { get; }
    public double Variance { get; }

    public double[] RawExecutionResults { get; } // milliseconds

    public int SampleSize { get; }
    public int NumWarmupIterations { get; }

    public double[] DataWithOutliersRemoved { get; } // milliseconds
    public double[] LowerOutliers { get; }
    public double[] UpperOutliers { get; }
    public int TotalNumOutliers { get; }

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
        return new PerformanceRunResult(displayName: testCaseId.DisplayName,
            mean: mean, stdDev: stdDev, variance: variance, median: median, rawExecutionResults: rawExecutionResults,
            sampleSize: executionSettings.SampleSize, numWarmupIterations: executionSettings.NumWarmupIterations, dataWithOutliersRemoved: cleanData,
            upperOutliers: upperOutliers.ToArray(), lowerOutliers: lowerOutliers.ToArray(), totalNumOutliers: totalNumOutliers);
    }
}