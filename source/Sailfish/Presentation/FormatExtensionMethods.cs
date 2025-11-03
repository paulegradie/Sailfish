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
                var clean = tr.DataWithOutliersRemoved ?? [];
                var n = clean.Length;
                var mean = tr.Mean;
                var stdDev = tr.StdDev;
                var standardError = n > 1 ? stdDev / System.Math.Sqrt(n) : 0;
                var ciList = PerformanceRunResult.ComputeConfidenceIntervals(mean, standardError, n, [0.95, 0.99]);
                var primary = ciList.First(x => System.Math.Abs(x.ConfidenceLevel - 0.95) < 1e-9);

                var perf = new PerformanceRunResult(
                    tr.DisplayName,
                    mean, stdDev, tr.Variance, tr.Median,
                    tr.RawExecutionResults ?? [],
                    tr.SampleSize, tr.NumWarmupIterations,
                    clean,
                    tr.UpperOutliers ?? [],
                    tr.LowerOutliers ?? [],
                    tr.TotalNumOutliers,
                    standardError, primary.ConfidenceLevel, primary.Lower, primary.Upper, primary.MarginOfError, ciList);

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