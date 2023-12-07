using Sailfish.Contracts.Public.Models;
using System.Collections.Generic;

namespace Sailfish.Analysis.SailDiff;

public interface IPerformanceRunResultAggregator
{
    AggregatedPerformanceResult? Aggregate(TestCaseId testCaseId, IReadOnlyCollection<PerformanceRunResult> data);
}

public class PerformanceRunResultAggregator : IPerformanceRunResultAggregator
{
    public AggregatedPerformanceResult? Aggregate(TestCaseId testCaseId, IReadOnlyCollection<PerformanceRunResult> data)
    {
        return data.Count switch
        {
            0 => null,
            _ => AggregatedPerformanceResult.Aggregate(testCaseId, data)
        };
    }
}