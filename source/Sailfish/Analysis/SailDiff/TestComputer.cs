using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sailfish.Contracts.Public;
using Sailfish.Extensions.Methods;
using Serilog;

namespace Sailfish.Analysis.SailDiff;

public class TestComputer : ITestComputer
{
    private readonly IStatisticalTestExecutor statisticalTestExecutor;
    private readonly IPerformanceRunResultAggregator aggregator;
    private readonly ILogger logger;

    public TestComputer(IStatisticalTestExecutor statisticalTestExecutor, IPerformanceRunResultAggregator aggregator, ILogger logger)
    {
        this.statisticalTestExecutor = statisticalTestExecutor;
        this.aggregator = aggregator;
        this.logger = logger;
    }

    public List<TestCaseResults> ComputeTest(TestData before, TestData after, SailDiffSettings settings)
    {
        var testCaseIdGroups = after
            .Data
            .Select(x => new TestCaseId(x.DisplayName))
            .GroupBy(x => x.DisplayName)
            .Select(x => x.First());
        var results = new ConcurrentBag<TestCaseResults>();
        Parallel.ForEach(
            testCaseIdGroups,
            new ParallelOptions()
            {
                MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism
            },
            (testCaseId) =>
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

                var result = statisticalTestExecutor.ExecuteStatisticalTest(
                    beforeCompiled.AggregatedRawExecutionResults,
                    afterCompiled.AggregatedRawExecutionResults,
                    settings);
                results.Add(new TestCaseResults(testCaseId, result));
            });

        if (settings.DisableOrdering || results.Count > 60)
        {
            return results.ToList();
        }

        try
        {
            return results.OrderByTestCaseId();
        }
        catch
        {
            return results
                .OrderByDescending(x => x.TestCaseId.DisplayName)
                .ToList();
        }
    }
}