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
                y.Exception is null
                    ? new CompiledTestCaseResult(
                        y.TestCaseId!,
                        y.GroupingId!,
                        y.PerformanceRunResult is not null
                            ? new PerformanceRunResult(
                                y.PerformanceRunResult.DisplayName,
                                y.PerformanceRunResult.Mean, y.PerformanceRunResult.StdDev, y.PerformanceRunResult.Variance, y.PerformanceRunResult.Median,
                                y.PerformanceRunResult.RawExecutionResults, y.PerformanceRunResult.SampleSize, y.PerformanceRunResult.NumWarmupIterations,
                                y.PerformanceRunResult.DataWithOutliersRemoved, y.PerformanceRunResult.UpperOutliers, y.PerformanceRunResult.LowerOutliers,
                                y.PerformanceRunResult.TotalNumOutliers
                            )
                            : null!)
                    : new CompiledTestCaseResult(
                        y.TestCaseId!,
                        y.GroupingId,
                        y.Exception!)
            ));
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