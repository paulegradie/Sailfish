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
        return trackingSummaries.Select(x =>
            new ClassExecutionSummary(
                x.TestClass,
                new ExecutionSettings(
                    x.ExecutionSettings.AsCsv,
                    x.ExecutionSettings.AsConsole,
                    x.ExecutionSettings.AsMarkdown,
                    x.ExecutionSettings.SampleSize,
                    x.ExecutionSettings.NumWarmupIterations),
                x.CompiledTestCaseResults
                    .Select(y =>
                        y.Exception is null
                            ? new CompiledTestCaseResult(
                                y.TestCaseId!,
                                y.GroupingId!,
                                y.PerformanceRunResult is not null
                                    ? new PerformanceRunResult(
                                        y.PerformanceRunResult.DisplayName, y.PerformanceRunResult.GlobalStart, y.PerformanceRunResult.GlobalEnd,
                                        y.PerformanceRunResult.GlobalDuration,
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
                    )));
    }


    public static IEnumerable<ClassExecutionSummaryTrackingFormat> ToTrackingFormat(this IEnumerable<IClassExecutionSummary> summaries)
    {
        return summaries.Select(x =>
            new ClassExecutionSummaryTrackingFormat(
                x.TestClass,
                new ExecutionSettingsTrackingFormat(
                    x.ExecutionSettings.AsCsv,
                    x.ExecutionSettings.AsConsole,
                    x.ExecutionSettings.AsMarkdown,
                    x.ExecutionSettings.NumWarmupIterations,
                    x.ExecutionSettings.SampleSize,
                    x.ExecutionSettings.DisableOverheadEstimation
                ),
                x.CompiledTestCaseResults
                    .Select(y => new CompiledTestCaseResultTrackingFormat(
                        y.GroupingId,
                        y.PerformanceRunResult is null
                            ? null
                            : new PerformanceRunResultTrackingFormat(
                                y.PerformanceRunResult.DisplayName, y.PerformanceRunResult.GlobalStart, y.PerformanceRunResult.GlobalEnd, y.PerformanceRunResult.GlobalDuration,
                                y.PerformanceRunResult.Mean, y.PerformanceRunResult.Median, y.PerformanceRunResult.StdDev, y.PerformanceRunResult.Variance,
                                y.PerformanceRunResult.RawExecutionResults, y.PerformanceRunResult.SampleSize, y.PerformanceRunResult.NumWarmupIterations,
                                y.PerformanceRunResult.DataWithOutliersRemoved, y.PerformanceRunResult.UpperOutliers, y.PerformanceRunResult.LowerOutliers,
                                y.PerformanceRunResult.TotalNumOutliers
                            ),
                        y.Exception,
                        y.TestCaseId
                    ))));
    }
}