using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;
using Sailfish.Contracts.Public;
using Sailfish.MathOps;
using Serilog;

namespace Sailfish.Analysis;

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
        List<TestCaseId> orderedTestCaseIds;
        try
        {
            orderedTestCaseIds = after
                .Data
                .Select(x => new TestCaseId(x.DisplayName))
                .OrderBy(x => x.TestCaseName.GetNamePart(0))
                .ThenBy(x => x.TestCaseName.GetNamePart(1))
                .ThenBy(x => x.TestCaseName.GetNamePart(2))
                .ThenBy(x => x.TestCaseName.GetNamePart(3))
                .ThenBy(x => x.TestCaseVariables.GetVariableIndex(0)?.Value)
                .ThenBy(x => x.TestCaseVariables.GetVariableIndex(1)?.Value)
                .ThenBy(x => x.TestCaseVariables.GetVariableIndex(2)?.Value)
                .ThenBy(x => x.TestCaseVariables.GetVariableIndex(3)?.Value)
                .ToList();
        }
        catch
        {
            orderedTestCaseIds = after
                .Data
                .Select(x => new TestCaseId(x.DisplayName))
                .OrderByDescending(x => x.DisplayName)
                .ToList();
        }

        var results = new List<TestCaseResults>();
        foreach (var testCaseId in orderedTestCaseIds.GroupBy(x => x.DisplayName).Select(x => x.First()))
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

            if (beforeCompiled is null || afterCompiled is null) continue; // this could be the first time we've ever run this test

            var result = statisticalTestExecutor.ExecuteStatisticalTest(
                beforeCompiled.RawExecutionResults,
                afterCompiled.RawExecutionResults,
                settings);

            results.Add(new TestCaseResults(testCaseId, result));
        }

        return results;
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