using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Methods;
using System.Collections.Concurrent;

namespace Sailfish.Analysis.SailDiff;

public interface IStatisticalTestComputer
{
    List<SailDiffResult> ComputeTest(TestData beforeTestData, TestData afterTestData, SailDiffSettings settings);
}

public class StatisticalTestComputer(IStatisticalTestExecutor statisticalTestExecutor, IPerformanceRunResultAggregator aggregator) : IStatisticalTestComputer
{
    private readonly IPerformanceRunResultAggregator aggregator = aggregator;
    private readonly IStatisticalTestExecutor statisticalTestExecutor = statisticalTestExecutor;

    /// <summary>
    ///     Compute a statistical test using the given TestData and SailDiffSettings
    /// </summary>
    /// <remarks>
    ///     All RawExecutionResult data is aggregated prior to test execution - if outlier detection is enabled, it is
    ///     applied to the aggregated RawExecutionResults
    /// </remarks>
    /// <param name="before"></param>
    /// <param name="after"></param>
    /// <param name="settings"></param>
    /// <returns></returns>
    public List<SailDiffResult> ComputeTest(TestData before, TestData after, SailDiffSettings settings)
    {
        var testCaseIdGroups = after
            .Data
            .Select(x => new TestCaseId(x.DisplayName))
            .GroupBy(x => x.DisplayName)
            .Select(x => x.First());
        var results = new ConcurrentBag<SailDiffResult>();
        Parallel.ForEach(
            testCaseIdGroups,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism
            },
            testCaseId =>
            {
                var afterCompiled = aggregator.Aggregate(
                    testCaseId,
                    after
                        .Data
                        .Where(x => new TestCaseId(x.DisplayName).Equals(testCaseId))
                        .ToList());

                var beforeCompiled = aggregator.Aggregate(
                    testCaseId,
                    before
                        .Data
                        .Where(x => new TestCaseId(x.DisplayName).Equals(testCaseId))
                        .ToList());

                if (beforeCompiled is null || afterCompiled is null) return;

                if (beforeCompiled.AggregatedRawExecutionResults.Length < 3 || afterCompiled.AggregatedRawExecutionResults.Length < 3) return;

                var result = statisticalTestExecutor.ExecuteStatisticalTest(
                    beforeCompiled.AggregatedRawExecutionResults,
                    afterCompiled.AggregatedRawExecutionResults,
                    settings);

                results.Add(new SailDiffResult(testCaseId, result));
            });

        if (settings.DisableOrdering || results.Count > 60) return [.. results];

        try
        {
            return results.OrderByTestCaseId();
        }
        catch
        {
            return
            [
                .. results
                    .OrderByDescending(x => x.TestCaseId.DisplayName)
            ];
        }
    }
}