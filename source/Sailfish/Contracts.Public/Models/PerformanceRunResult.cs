using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis;
using Sailfish.Execution;

namespace Sailfish.Contracts.Public.Models;

public class PerformanceRunResult(
    string displayName,
    double mean,
    double stdDev,
    double variance,
    double median,
    double[] rawExecutionResults,
    int sampleSize,
    int numWarmupIterations,
    double[] dataWithOutliersRemoved,
    double[] upperOutliers,
    double[] lowerOutliers,
    int totalNumOutliers,
    double standardError = 0.0,
    double confidenceLevel = 0.95,
    double confidenceIntervalLower = 0.0,
    double confidenceIntervalUpper = 0.0,
    double marginOfError = 0.0)
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

    // NEW: Confidence Interval Properties
    public double StandardError { get; } = standardError;
    public double ConfidenceLevel { get; } = confidenceLevel;
    public double ConfidenceIntervalLower { get; } = confidenceIntervalLower;
    public double ConfidenceIntervalUpper { get; } = confidenceIntervalUpper;
    public double MarginOfError { get; } = marginOfError;
    public double ConfidenceIntervalWidth => ConfidenceIntervalUpper - ConfidenceIntervalLower;

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

        // Calculate confidence interval
        var standardError = cleanData.Count > 1 ? stdDev / Math.Sqrt(cleanData.Count) : 0;
        var confidenceLevel = executionSettings.ConfidenceLevel;
        var tValue = GetTValue(confidenceLevel, cleanData.Count - 1);
        var marginOfError = tValue * standardError;
        var ciLower = mean - marginOfError;
        var ciUpper = mean + marginOfError;

        return new PerformanceRunResult(testCaseId.DisplayName,
            mean, stdDev, variance, median, rawExecutionResults,
            executionSettings.SampleSize, executionSettings.NumWarmupIterations, cleanData,
            upperOutliers.ToArray(), lowerOutliers.ToArray(), totalNumOutliers,
            standardError, confidenceLevel, ciLower, ciUpper, marginOfError);
    }

    /// <summary>
    /// Gets the critical t-value for the specified confidence level and degrees of freedom.
    /// </summary>
    private static double GetTValue(double confidenceLevel, int degreesOfFreedom)
    {
        // For large samples (df >= 30), use normal approximation
        if (degreesOfFreedom >= 30)
        {
            return confidenceLevel switch
            {
                0.90 => 1.645,
                0.95 => 1.960,
                0.99 => 2.576,
                0.999 => 3.291,
                _ => 1.960 // Default to 95% CI
            };
        }

        // For small samples, use conservative t-values (simplified lookup table)
        return degreesOfFreedom switch
        {
            1 => confidenceLevel >= 0.95 ? 12.706 : 6.314,
            2 => confidenceLevel >= 0.95 ? 4.303 : 2.920,
            3 => confidenceLevel >= 0.95 ? 3.182 : 2.353,
            4 => confidenceLevel >= 0.95 ? 2.776 : 2.132,
            5 => confidenceLevel >= 0.95 ? 2.571 : 2.015,
            6 => confidenceLevel >= 0.95 ? 2.447 : 1.943,
            7 => confidenceLevel >= 0.95 ? 2.365 : 1.895,
            8 => confidenceLevel >= 0.95 ? 2.306 : 1.860,
            9 => confidenceLevel >= 0.95 ? 2.262 : 1.833,
            10 => confidenceLevel >= 0.95 ? 2.228 : 1.812,
            _ when degreesOfFreedom <= 20 => confidenceLevel >= 0.95 ? 2.086 : 1.725,
            _ => confidenceLevel >= 0.95 ? 2.000 : 1.680 // Conservative estimate
        };
    }
}