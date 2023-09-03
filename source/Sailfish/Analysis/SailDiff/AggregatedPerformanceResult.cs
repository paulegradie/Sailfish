using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public;

namespace Sailfish.Analysis.SailDiff;

public class AggregatedPerformanceResult
{
    private AggregatedPerformanceResult(
        double[] aggregatedRawExecutionResults,
        string displayName,
        int numIterations,
        int numWarmupIterations)
    {
        AggregatedRawExecutionResults = aggregatedRawExecutionResults;
        DisplayName = displayName;
        NumIterations = numIterations;
        NumWarmupIterations = numWarmupIterations;
    }

    private const double Tolerance = 0.000000001;
    public string DisplayName { get; init; }

    public double[] AggregatedRawExecutionResults { get; init; } // milliseconds

    public int NumIterations { get; set; }
    public int NumWarmupIterations { get; set; }

    public static AggregatedPerformanceResult Aggregate(TestCaseId testCaseId, IReadOnlyCollection<PerformanceRunResult> data)
    {
        var allRawData = data.SelectMany(x => x.RawExecutionResults).ToArray();
        return new AggregatedPerformanceResult(
            aggregatedRawExecutionResults: allRawData,
            displayName: testCaseId.DisplayName,
            numIterations: data.First().NumSamples,
            numWarmupIterations: data.First().NumWarmups);
    }
}