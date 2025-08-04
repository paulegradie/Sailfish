using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;

namespace Sailfish.Extensions.Types;

public class TrackingFileDataList : List<List<IClassExecutionSummary>>
{
    public ICompiledTestCaseResult? FindFirstMatchingTestCaseId(TestCaseId displayName)
    {
        return this.Select(preloadedLastRun =>
                preloadedLastRun
                    .SelectMany(x => x.CompiledTestCaseResults)
                    .FirstOrDefault(x =>
                        x.TestCaseId is not null && new TestCaseId(x.TestCaseId?.DisplayName!).Equals(displayName)))
            .Where(x => x?.PerformanceRunResult is not null)
            .Cast<ICompiledTestCaseResult>()
            .FirstOrDefault();
    }
    
}