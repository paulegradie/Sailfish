using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Execution;
using Sailfish.Statistics;

namespace Sailfish.Analysis;

public static class TrackingFileConversionExtensionMethods
{
    #region ToTrackingFormat

    public static IEnumerable<ExecutionSummaryTrackingFormatV1> ToTrackingFormat(this IEnumerable<IExecutionSummary> executionSummaries)
    {
        return executionSummaries.Select(x =>
            new ExecutionSummaryTrackingFormatV1(
                x.Type,
                x.Settings.ToTrackingFormat(),
                x.CompiledTestCaseResults.ToTrackingFormat()
            ));
    }

    public static IEnumerable<CompiledTestCaseResultTrackingFormatV1> ToTrackingFormat(this IEnumerable<ICompiledTestCaseResult> compiledTestCaseResult)
    {
        return compiledTestCaseResult.Select(
            x => new CompiledTestCaseResultTrackingFormatV1(
                x.GroupingId!,
                x.PerformanceRunResult!.ToTrackingFormat(),
                x.Exceptions,
                x.TestCaseId!));
    }

    public static PerformanceRunResultTrackingFormatV1 ToTrackingFormat(this PerformanceRunResult r)
    {
        return new PerformanceRunResultTrackingFormatV1(
            r.DisplayName,
            r.Mean,
            r.Median,
            r.StdDev,
            r.Variance,
            r.GlobalDuration,
            r.GlobalStart,
            r.GlobalEnd,
            r.RawExecutionResults,
            r.NumIterations,
            r.NumWarmupIterations,
            r.DataWithOutliersRemoved,
            r.LowerOutliers,
            r.UpperOutliers,
            r.TotalNumOutliers
        );
    }

    public static ExecutionSettingsTrackingFormat ToTrackingFormat(this IExecutionSettings executionSettings)
    {
        return new ExecutionSettingsTrackingFormat(
            executionSettings.AsCsv,
            executionSettings.AsConsole,
            executionSettings.AsMarkdown,
            executionSettings.NumWarmupIterations,
            executionSettings.NumIterations,
            executionSettings.DisableOverheadEstimation);
    }

    #endregion


    #region ToSummaryFormat

    public static IEnumerable<IExecutionSummary> ToSummaryFormat(this IEnumerable<ExecutionSummaryTrackingFormatV1> trackingData)
    {
        return trackingData
            .Select(x => new ExecutionSummary(x.Type, x.CompiledTestCaseResults.ToSummaryFormat()));
    }

    public static IEnumerable<ICompiledTestCaseResult> ToSummaryFormat(this IEnumerable<CompiledTestCaseResultTrackingFormatV1> trackingData)
    {
        return trackingData
            .Select(x => new CompiledTestCaseResult(x.TestCaseId!, x.GroupingId!, x.PerformanceRunResultTrackingFormatV1.ToSummaryFormat()));
    }

    public static PerformanceRunResult ToSummaryFormat(this PerformanceRunResultTrackingFormatV1 trackingData)
    {
        return new PerformanceRunResult(
            trackingData.DisplayName,
            trackingData.GlobalStart, trackingData.GlobalEnd, trackingData.GlobalDuration, trackingData.Mean, trackingData.StdDev,
            trackingData.Variance, trackingData.Median, trackingData.RawExecutionResults, trackingData.NumIterations,
            trackingData.NumWarmupIterations, trackingData.DataWithOutliersRemoved, trackingData.UpperOutliers, trackingData.LowerOutliers, trackingData.TotalNumOutliers);
    }

    #endregion
}