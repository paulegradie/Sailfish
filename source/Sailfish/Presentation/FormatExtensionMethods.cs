using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Execution;
using Sailfish.Statistics;

namespace Sailfish.Presentation;

public static class FormatExtensionMethods
{
    public static IEnumerable<IClassExecutionSummary> ToSummaryFormat(this IEnumerable<ClassExecutionSummaryTrackingFormat> trackingSummaries)
    {
        return trackingSummaries.Select(classSummaryTracking =>
            new ClassExecutionSummary(
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
                            y.TestCaseId,
                            y.GroupingId,
                            y.Exception!)
                )
            )
        );
    }


    public static IEnumerable<ClassExecutionSummaryTrackingFormat> ToTrackingFormat(this IEnumerable<IClassExecutionSummary> summaries)
    {
        return summaries.Select(classSummary =>
            new ClassExecutionSummaryTrackingFormat(
                classSummary.TestClass,
                new ExecutionSettingsTrackingFormat(
                    classSummary.ExecutionSettings.AsCsv,
                    classSummary.ExecutionSettings.AsConsole,
                    classSummary.ExecutionSettings.AsMarkdown,
                    classSummary.ExecutionSettings.NumWarmupIterations,
                    classSummary.ExecutionSettings.SampleSize,
                    classSummary.ExecutionSettings.DisableOverheadEstimation
                ),
                classSummary.CompiledTestCaseResults.Select(testCaseResult => new CompiledTestCaseResultTrackingFormat(
                        testCaseResult.GroupingId,
                        testCaseResult.PerformanceRunResult is null
                            ? null
                            : new PerformanceRunResultTrackingFormat(
                                testCaseResult.PerformanceRunResult.DisplayName,
                                testCaseResult.PerformanceRunResult.Mean, testCaseResult.PerformanceRunResult.Median, testCaseResult.PerformanceRunResult.StdDev,
                                testCaseResult.PerformanceRunResult.Variance,
                                testCaseResult.PerformanceRunResult.RawExecutionResults, testCaseResult.PerformanceRunResult.SampleSize,
                                testCaseResult.PerformanceRunResult.NumWarmupIterations,
                                testCaseResult.PerformanceRunResult.DataWithOutliersRemoved, testCaseResult.PerformanceRunResult.UpperOutliers,
                                testCaseResult.PerformanceRunResult.LowerOutliers,
                                testCaseResult.PerformanceRunResult.TotalNumOutliers
                            ),
                        testCaseResult.Exception,
                        testCaseResult.TestCaseId
                    )
                )
            )
        );
    }
}