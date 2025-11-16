using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using Sailfish.Analysis;
using Sailfish.Execution;

namespace Sailfish.Contracts.Public.Models;

public class PerformanceRunResult
{
    public PerformanceRunResult(
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
        double marginOfError = 0.0,
        IReadOnlyList<ConfidenceIntervalResult>? confidenceIntervals = null)
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
        StandardError = standardError;
        ConfidenceLevel = confidenceLevel;
        ConfidenceIntervalLower = confidenceIntervalLower;
        ConfidenceIntervalUpper = confidenceIntervalUpper;
        MarginOfError = marginOfError;
        ConfidenceIntervals = confidenceIntervals ?? Array.Empty<ConfidenceIntervalResult>();
    }

    public string DisplayName { get; }
    public double Mean { get; }
    public double Median { get; }
    public double StdDev { get; }
    public double Variance { get; }

    public double[] RawExecutionResults { get; }

    public int SampleSize { get; }
    public int NumWarmupIterations { get; }

    public double[] DataWithOutliersRemoved { get; }
    public double[] LowerOutliers { get; }
    public double[] UpperOutliers { get; }
    public int TotalNumOutliers { get; }

    // NEW: Confidence Interval Properties
    public double StandardError { get; }
    public double ConfidenceLevel { get; }
    public double ConfidenceIntervalLower { get; }
    public double ConfidenceIntervalUpper { get; }
    public double MarginOfError { get; set; }
    public double ConfidenceIntervalWidth => ConfidenceIntervalUpper - ConfidenceIntervalLower;

    // Centralized multi-level CIs
    public IReadOnlyList<ConfidenceIntervalResult> ConfidenceIntervals { get; }


    // Statistical validation warnings (optional)
    public ValidationResult? Validation { get; set; }

    // Convenience properties for CSV and simple consumers
    public double Ci95MarginOfError => GetMarginFor(0.95);
    public double Ci99MarginOfError => GetMarginFor(0.99);

    private double GetMarginFor(double level)
    {
        var ci = ConfidenceIntervals?.FirstOrDefault(x => Math.Abs(x.ConfidenceLevel - level) < 1e-9);
        if (ci != null) return ci.MarginOfError;
        // Fallback: if this instance represents the single-level CI and matches the query
        if (Math.Abs(ConfidenceLevel - level) < 1e-9) return MarginOfError;
        // As a last resort, compute from SE if possible
        var n = DataWithOutliersRemoved?.Length ?? 0;
        if (n <= 1 || StandardError < 0.000000001) return 0;
        var dof = Math.Max(1, n - 1);
        var t = StudentT.InvCDF(0, 1, dof, 0.5 + level / 2.0);
        return t * StandardError;
    }

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
        // Choose outlier handling path: legacy (RemoveAll) vs configurable (settings-driven)
        ProcessedStatisticalTestData processed;
        if (executionSettings.UseConfigurableOutlierDetection)
        {
            var cfg = new ConfigurableOutlierDetector();
            processed = cfg.DetectOutliers(executionIterations, executionSettings.OutlierStrategy);
        }
        else
        {
            var detector = new SailfishOutlierDetector();
            processed = detector.DetectOutliers(executionIterations);
        }

        var rawExecutionResults = processed.OriginalData;
        var cleanData = processed.DataWithOutliersRemoved;
        var lowerOutliers = processed.LowerOutliers;
        var upperOutliers = processed.UpperOutliers;
        var totalNumOutliers = processed.TotalNumOutliers;

        var mean = cleanData.Mean();
        var median = cleanData.Median();
        var stdDev = executionIterations.Count > 1 ? cleanData.StandardDeviation() : 0;
        var variance = executionIterations.Count > 1 ? cleanData.Variance() : 0;

        // Calculate confidence intervals centrally
        var n = cleanData.Length;
        var standardError = n > 1 ? stdDev / Math.Sqrt(n) : 0;
        var configuredLevels = (executionSettings as ExecutionSettings)?.ReportConfidenceLevels
                                ?? new List<double> { executionSettings.ConfidenceLevel };
        var ciList = ComputeConfidenceIntervals(mean, standardError, n, configuredLevels);

        // Preserve single-level fields for backward compatibility (use executionSettings.ConfidenceLevel)
        var primaryLevel = executionSettings.ConfidenceLevel;
        var primary = ciList.FirstOrDefault(x => Math.Abs(x.ConfidenceLevel - primaryLevel) < 1e-9)
                     ?? ComputeConfidenceIntervals(mean, standardError, n, [primaryLevel])[0];

        var pr = new PerformanceRunResult(testCaseId.DisplayName,
            mean, stdDev, variance, median, rawExecutionResults,
            executionSettings.SampleSize, executionSettings.NumWarmupIterations, cleanData,
            upperOutliers.ToArray(), lowerOutliers.ToArray(), totalNumOutliers,
            standardError, primary.ConfidenceLevel, primary.Lower, primary.Upper, primary.MarginOfError,
            ciList);

        // Best-effort statistical validation
        try
        {
            var validation = new StatisticalValidator().Validate(pr, executionSettings);
            pr.Validation = validation;
        }
        catch
        {
            // ignore validation failures to preserve robustness
        }

        return pr;
    }

    /// <summary>
    /// Centralized CI computation for one or more confidence levels using Student's t distribution.
    /// </summary>
    public static IReadOnlyList<ConfidenceIntervalResult> ComputeConfidenceIntervals(double mean, double standardError, int n, IEnumerable<double> confidenceLevels)
    {
        var result = new List<ConfidenceIntervalResult>();
        if (n <= 1 || standardError < 0.000000001)
        {
            foreach (var cl in confidenceLevels.Distinct())
            {
                result.Add(new ConfidenceIntervalResult(cl, 0, mean, mean));
            }
            return result;
        }

        var dof = Math.Max(1, n - 1);
        foreach (var cl in confidenceLevels.Distinct().OrderBy(x => x))
        {
            var t = StudentT.InvCDF(0, 1, dof, 0.5 + cl / 2.0);
            var moe = t * standardError;
            result.Add(new ConfidenceIntervalResult(cl, moe, mean - moe, mean + moe));
        }

        return result;
    }
}