using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Execution;

namespace Sailfish.Presentation;

public static class FormatExtensionMethods
{
    public static IClassExecutionSummary ToSummaryFormat(this ClassExecutionSummaryTrackingFormat classSummaryTracking)
    {
        return new ClassExecutionSummary(
            classSummaryTracking.TestClass,
            new ExecutionSettings(
                classSummaryTracking.ExecutionSettings.AsCsv,
                classSummaryTracking.ExecutionSettings.AsConsole,
                classSummaryTracking.ExecutionSettings.AsMarkdown,
                classSummaryTracking.ExecutionSettings.SampleSize,
                classSummaryTracking.ExecutionSettings.NumWarmupIterations),
            classSummaryTracking.CompiledTestCaseResults.Select(y =>
            {
                if (y.Exception is not null)
                {
                    return new CompiledTestCaseResult(
                        y.TestCaseId!,
                        y.GroupingId,
                        y.Exception!);
                }

                if (y.PerformanceRunResult is null)
                {
                    return new CompiledTestCaseResult(
                        y.TestCaseId!,
                        y.GroupingId!,
                        (PerformanceRunResult)null!);
                }

                var tr = y.PerformanceRunResult;
                var clean = tr.DataWithOutliersRemoved ?? System.Array.Empty<double>();
                var n = clean.Length;
                var mean = tr.Mean;
                var stdDev = tr.StdDev;
                var standardError = n > 1 ? stdDev / System.Math.Sqrt(n) : 0;
                var confidenceLevel = 0.95; // tracking format doesn't store this; default

                double GetTValue(double cl, int dof)
                {
                    if (dof >= 30)
                    {
                        return cl switch
                        {
                            0.90 => 1.645,
                            0.95 => 1.960,
                            0.99 => 2.576,
                            0.999 => 3.291,
                            _ => 1.960
                        };
                    }

                    return dof switch
                    {
                        1 => cl >= 0.95 ? 12.706 : 6.314,
                        2 => cl >= 0.95 ? 4.303 : 2.920,
                        3 => cl >= 0.95 ? 3.182 : 2.353,
                        4 => cl >= 0.95 ? 2.776 : 2.132,
                        5 => cl >= 0.95 ? 2.571 : 2.015,
                        6 => cl >= 0.95 ? 2.447 : 1.943,
                        7 => cl >= 0.95 ? 2.365 : 1.895,
                        8 => cl >= 0.95 ? 2.306 : 1.860,
                        9 => cl >= 0.95 ? 2.262 : 1.833,
                        10 => cl >= 0.95 ? 2.228 : 1.812,
                        _ when dof <= 20 => cl >= 0.95 ? 2.086 : 1.725,
                        _ => cl >= 0.95 ? 2.000 : 1.680
                    };
                }

                var tValue = GetTValue(confidenceLevel, n - 1);
                var marginOfError = tValue * standardError;
                var ciLower = mean - marginOfError;
                var ciUpper = mean + marginOfError;

                var perf = new PerformanceRunResult(
                    tr.DisplayName,
                    mean, stdDev, tr.Variance, tr.Median,
                    tr.RawExecutionResults ?? System.Array.Empty<double>(),
                    tr.SampleSize, tr.NumWarmupIterations,
                    clean,
                    tr.UpperOutliers ?? System.Array.Empty<double>(),
                    tr.LowerOutliers ?? System.Array.Empty<double>(),
                    tr.TotalNumOutliers,
                    standardError, confidenceLevel, ciLower, ciUpper, marginOfError);

                return new CompiledTestCaseResult(
                    y.TestCaseId!,
                    y.GroupingId!,
                    perf);
            }));
    }

    public static IEnumerable<IClassExecutionSummary> ToSummaryFormat(this IEnumerable<ClassExecutionSummaryTrackingFormat> trackingSummaries)
    {
        return trackingSummaries.Select(ToSummaryFormat);
    }

    public static ClassExecutionSummaryTrackingFormat ToTrackingFormat(this IClassExecutionSummary classSummary)
    {
        return new ClassExecutionSummaryTrackingFormat(
            classSummary.TestClass,
            new ExecutionSettingsTrackingFormat(
                classSummary.ExecutionSettings.AsCsv,
                classSummary.ExecutionSettings.AsConsole,
                classSummary.ExecutionSettings.AsMarkdown,
                classSummary.ExecutionSettings.NumWarmupIterations,
                classSummary.ExecutionSettings.SampleSize,
                classSummary.ExecutionSettings.DisableOverheadEstimation
            ),
            classSummary.CompiledTestCaseResults.Select(testCaseResult
                => new CompiledTestCaseResultTrackingFormat(
                    testCaseResult.GroupingId,
                    testCaseResult.PerformanceRunResult?.ToTrackingFormat(),
                    testCaseResult.Exception,
                    testCaseResult.TestCaseId)
            )
        );
    }

    public static PerformanceRunResultTrackingFormat ToTrackingFormat(this PerformanceRunResult runResult)
    {
        return new PerformanceRunResultTrackingFormat(
            runResult.DisplayName,
            runResult.Mean,
            runResult.Median,
            runResult.StdDev,
            runResult.Variance,
            runResult.RawExecutionResults,
            runResult.SampleSize,
            runResult.NumWarmupIterations,
            runResult.DataWithOutliersRemoved,
            runResult.UpperOutliers,
            runResult.LowerOutliers,
            runResult.TotalNumOutliers);
    }

    public static IEnumerable<ClassExecutionSummaryTrackingFormat> ToTrackingFormat(this IEnumerable<IClassExecutionSummary> summaries)
    {
        return summaries.Select(ToTrackingFormat);
    }
}