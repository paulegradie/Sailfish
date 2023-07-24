using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        if (settings.DisableOrdering)
        {
            return results.ToList();
        }

        try
        {
            // TODO: improve this 
            // var orderedGroups = new ConcurrentDictionary<string, TestCaseResults>();
            // var groups = results
            //     .GroupBy(x => x.TestCaseId.TestCaseName.Name);
            //
            // Parallel.ForEach(groups, new ParallelOptions() { MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism }, (group) =>
            // {
            //     var orderedGroup = group
            //         .OrderBy(x => x.TestCaseId.TestCaseVariables.GetVariableIndex(0)?.Value)
            //         .ThenBy(x => x.TestCaseId.TestCaseVariables.GetVariableIndex(1)?.Value)
            //         .ThenBy(x => x.TestCaseId.TestCaseVariables.GetVariableIndex(2)?.Value)
            //         .ThenBy(x => x.TestCaseId.TestCaseVariables.GetVariableIndex(3)?.Value);
            //     orderedGroups.AddRange(orderedGroup);
            // });
            //
            // foreach (var group in groups)
            // {
            //     orderedGroups.AddRange(
            //         group.OrderBy());
            // }

            return results
                .OrderBy(x => x.TestCaseId.TestCaseName.GetNamePart(0))
                .ThenBy(x => x.TestCaseId.TestCaseName.GetNamePart(1))
                .ThenBy(x => x.TestCaseId.TestCaseName.GetNamePart(2))
                .ThenBy(x => x.TestCaseId.TestCaseName.GetNamePart(3))
                .ThenBy(x => x.TestCaseId.TestCaseVariables.GetVariableIndex(0)?.Value)
                .ThenBy(x => x.TestCaseId.TestCaseVariables.GetVariableIndex(1)?.Value)
                .ThenBy(x => x.TestCaseId.TestCaseVariables.GetVariableIndex(2)?.Value)
                .ThenBy(x => x.TestCaseId.TestCaseVariables.GetVariableIndex(3)?.Value)
                .ToList();
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
//
// public class TestCaseIdComparer : IComparer<TestCaseId>
// {
//     public int Compare(TestCaseId? x, TestCaseId? y)
//     {
//         if (x is null && y is null) return 0;
//         if (x is null) return -1;
//         if (y is null) return 1;
//
//         // Compare by parts of the TestCaseName
//         for (var i = 0; i < Math.Max(x.TestCaseName.Parts.Count, y.TestCaseName.Parts.Count); i++)
//         {
//             var xPart = i < x.TestCaseName.Parts.Count ? x.TestCaseName.Parts[i] : string.Empty;
//             var yPart = i < y.TestCaseName.Parts.Count ? y.TestCaseName.Parts[i] : string.Empty;
//             var partComparison = string.Compare(xPart, yPart, StringComparison.OrdinalIgnoreCase);
//             if (partComparison != 0)
//             {
//                 return partComparison;
//             }
//         }
//
//         // Compare by TestCaseVariables
//         for (var i = 0; i < Math.Max(x.TestCaseVariables.Variables.Count(), y.TestCaseVariables.Variables.Count()); i++)
//         {
//             var xVar = x.TestCaseVariables.GetVariableIndex(i)?.Value as string;
//             var yVar = y.TestCaseVariables.GetVariableIndex(i)?.Value as string;
//             var varComparison = string.Compare(xVar, yVar, StringComparison.OrdinalIgnoreCase);
//             if (varComparison != 0)
//             {
//                 return varComparison;
//             }
//         }
//
//         return 0; // The objects are equal
//     }
// }