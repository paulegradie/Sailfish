using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MathNet.Numerics.Statistics;
using Sailfish.Contracts.Public;
using Sailfish.MathOps;
using Serilog;

namespace Sailfish.Analysis.Saildiff;

public class TestComputer : ITestComputer
{
    private readonly IStatisticalTestExecutor statisticalTestExecutor;
    private readonly ILogger logger;

    public TestComputer(IStatisticalTestExecutor statisticalTestExecutor, ILogger logger)
    {
        this.statisticalTestExecutor = statisticalTestExecutor;
        this.logger = logger;
    }

    public List<TestCaseResults> ComputeTest(TestData before, TestData after, TestSettings settings)
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
                var afterCompiled = Aggregate(
                    testCaseId,
                    settings,
                    after
                        .Data
                        .Where(x => new TestCaseId(x.DisplayName).Equals(testCaseId))
                        .ToList());

                var beforeCompiled = Aggregate(
                    testCaseId,
                    settings,
                    before
                        .Data
                        .Where(x => new TestCaseId(x.DisplayName).Equals(testCaseId))
                        .ToList());

                if (beforeCompiled is null || afterCompiled is null) return;
                var result = statisticalTestExecutor.ExecuteStatisticalTest(
                    beforeCompiled.RawExecutionResults,
                    afterCompiled.RawExecutionResults,
                    settings);

                results.Add(new TestCaseResults(testCaseId, result));
            });

        if (settings.DisableOrdering || results.Count > 60)
        {
            return results.ToList();
        }

        try
        {
            return results.OrderBy(x => x.TestCaseId, new TestCaseIdComparer()).ToList();
        }
        catch
        {
            return results
                .OrderByDescending(x => x.TestCaseId.DisplayName)
                .ToList();
        }
    }

    private static DescriptiveStatisticsResult? Aggregate(TestCaseId testCaseId, TestSettings settings, IReadOnlyCollection<DescriptiveStatisticsResult> data)
    {
        switch (data.Count)
        {
            case 0:
                return null;
            case 1:
                return data.Single();
            default:
                var allRawData = data.SelectMany(x => x.RawExecutionResults).ToArray();
                return new DescriptiveStatisticsResult
                {
                    RawExecutionResults = allRawData,
                    DisplayName = testCaseId.DisplayName,
                    GlobalDuration = data.Select(x => x.GlobalDuration).Mean(),
                    GlobalStart = data.OrderBy(x => x.GlobalStart).First().GlobalStart,
                    GlobalEnd = data.OrderBy(x => x.GlobalEnd).First().GlobalEnd,
                    Mean = settings.UseInnerQuartile ? ComputeQuartiles.GetInnerQuartileValues(allRawData).Mean() : allRawData.Mean(),
                    Median = settings.UseInnerQuartile ? ComputeQuartiles.GetInnerQuartileValues(allRawData).Median() : allRawData.Median(),
                    Variance = settings.UseInnerQuartile ? ComputeQuartiles.GetInnerQuartileValues(allRawData).Variance() : allRawData.Variance(),
                    StdDev = settings.UseInnerQuartile ? ComputeQuartiles.GetInnerQuartileValues(allRawData).StandardDeviation() : allRawData.StandardDeviation(),
                };
        }
    }
}