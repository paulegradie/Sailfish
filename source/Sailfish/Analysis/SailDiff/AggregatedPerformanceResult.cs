using Sailfish.Contracts.Public.Models;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.SailDiff;

public class AggregatedPerformanceResult
{
    private AggregatedPerformanceResult(
        double[] aggregatedRawExecutionResults,
        string displayName,
        int sampleSize,
        int numWarmupIterations)
    {
        AggregatedRawExecutionResults = aggregatedRawExecutionResults;
        DisplayName = displayName;
        SampleSize = sampleSize;
        NumWarmupIterations = numWarmupIterations;
    }

    public string DisplayName { get; init; }

    public double[] AggregatedRawExecutionResults { get; init; } // milliseconds

    public int SampleSize { get; set; }
    public int NumWarmupIterations { get; set; }

    public static AggregatedPerformanceResult Aggregate(TestCaseId testCaseId, IReadOnlyCollection<PerformanceRunResult> data)
    {
        var allRawData = data.SelectMany(x => x.RawExecutionResults).ToArray();
        return new AggregatedPerformanceResult(
            allRawData,
            testCaseId.DisplayName,
            data.First().SampleSize,
            data.First().NumWarmupIterations);
    }
}