using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Analysis.SailDiff;
using Sailfish.Attributes;

namespace Sailfish.Execution;

internal interface IMethodComparisonCoordinator
{
    Task<List<MethodComparisonResult>> ExecuteComparisons(
        List<TestCaseExecutionResult> executionResults,
        CancellationToken cancellationToken = default);
}

internal class MethodComparisonCoordinator : IMethodComparisonCoordinator
{
    private readonly IStatisticalTestComputer statisticalTestComputer;
    private readonly IPerformanceRunResultAggregator aggregator;

    public MethodComparisonCoordinator(
        IStatisticalTestComputer statisticalTestComputer,
        IPerformanceRunResultAggregator aggregator)
    {
        this.statisticalTestComputer = statisticalTestComputer ?? throw new ArgumentNullException(nameof(statisticalTestComputer));
        this.aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
    }

    public async Task<List<MethodComparisonResult>> ExecuteComparisons(
        List<TestCaseExecutionResult> executionResults,
        CancellationToken cancellationToken = default)
    {
        var comparisonGroups = IdentifyComparisonGroups(executionResults);
        var results = new List<MethodComparisonResult>();

        foreach (var group in comparisonGroups)
        {
            var comparisonResult = await ExecuteGroupComparison(group, executionResults, cancellationToken);
            if (comparisonResult != null)
            {
                results.Add(comparisonResult);
            }
        }

        return results;
    }

    private List<TestCaseComparisonGroup> IdentifyComparisonGroups(List<TestCaseExecutionResult> executionResults)
    {
        var groupedResults = new Dictionary<string, List<TestCaseExecutionResult>>();

        foreach (var result in executionResults.Where(r => r.IsSuccess))
        {
            var comparisonAttribute = result.TestInstanceContainer?.ExecutionMethod
                ?.GetCustomAttribute<SailfishMethodComparisonAttribute>();

            if (comparisonAttribute != null)
            {
                if (!groupedResults.ContainsKey(comparisonAttribute.ComparisonGroup))
                {
                    groupedResults[comparisonAttribute.ComparisonGroup] = new List<TestCaseExecutionResult>();
                }
                groupedResults[comparisonAttribute.ComparisonGroup].Add(result);
            }
        }

        return groupedResults
            .Where(kvp => kvp.Value.Count >= 2) // Need at least 2 methods to compare
            .Select(kvp => CreateComparisonGroup(kvp.Key, kvp.Value))
            .ToList();
    }

    private TestCaseComparisonGroup CreateComparisonGroup(string groupName, List<TestCaseExecutionResult> results)
    {
        var testCaseIds = results.Select(r => r.TestInstanceContainer!.TestCaseId).ToList();

        // Find baseline method if specified
        string? baselineMethod = null;
        double significanceLevel = 0.05;

        var firstResult = results.First();
        var comparisonAttribute = firstResult.TestInstanceContainer?.ExecutionMethod
            ?.GetCustomAttribute<SailfishMethodComparisonAttribute>();

        if (comparisonAttribute != null)
        {
            baselineMethod = comparisonAttribute.BaselineMethod;
            significanceLevel = comparisonAttribute.SignificanceLevel;
        }

        return new TestCaseComparisonGroup(groupName, testCaseIds, baselineMethod)
        {
            SignificanceLevel = significanceLevel
        };
    }

    private Task<MethodComparisonResult?> ExecuteGroupComparison(
        TestCaseComparisonGroup group,
        List<TestCaseExecutionResult> allResults,
        CancellationToken cancellationToken)
    {
        var groupResults = allResults
            .Where(r => group.TestCases.Any(tc => tc.Equals(r.TestInstanceContainer!.TestCaseId)))
            .ToList();

        if (groupResults.Count < 2) return Task.FromResult<MethodComparisonResult?>(null);

        // Convert to TestData format for SailDiff
        var testDataPairs = CreateTestDataPairs(groupResults);
        var pairwiseComparisons = new List<SailDiffResult>();

        // Perform pairwise comparisons
        var settings = new SailDiffSettings(
            alpha: group.SignificanceLevel,
            maxDegreeOfParallelism: Environment.ProcessorCount);

        foreach (var (before, after, beforeId, afterId) in testDataPairs)
        {
            var comparison = statisticalTestComputer.ComputeTest(before, after, settings);
            pairwiseComparisons.AddRange(comparison);
        }

        // Calculate rankings
        var rankings = CalculateMethodRankings(groupResults, group);

        return Task.FromResult<MethodComparisonResult?>(new MethodComparisonResult(group, pairwiseComparisons, rankings));
    }

    private List<(TestData before, TestData after, TestCaseId beforeId, TestCaseId afterId)> CreateTestDataPairs(
        List<TestCaseExecutionResult> results)
    {
        var pairs = new List<(TestData, TestData, TestCaseId, TestCaseId)>();

        for (int i = 0; i < results.Count; i++)
        {
            for (int j = i + 1; j < results.Count; j++)
            {
                var result1 = results[i];
                var result2 = results[j];

                var testData1 = ConvertToTestData(result1);
                var testData2 = ConvertToTestData(result2);

                pairs.Add((testData1, testData2, result1.TestInstanceContainer!.TestCaseId, result2.TestInstanceContainer!.TestCaseId));
            }
        }

        return pairs;
    }

    private TestData ConvertToTestData(TestCaseExecutionResult result)
    {
        if (result.PerformanceTimerResults == null || result.TestInstanceContainer == null || result.ExecutionSettings == null)
        {
            return new TestData(new List<string>(), new List<PerformanceRunResult>());
        }

        var performanceRunResult = PerformanceRunResult.ConvertFromPerfTimer(
            result.TestInstanceContainer.TestCaseId,
            result.PerformanceTimerResults,
            result.ExecutionSettings);

        var testIds = new List<string> { result.TestInstanceContainer.TestCaseId.DisplayName };
        return new TestData(testIds, new List<PerformanceRunResult> { performanceRunResult });
    }

    private List<MethodRanking> CalculateMethodRankings(
        List<TestCaseExecutionResult> results,
        TestCaseComparisonGroup group)
    {
        var rankings = new List<MethodRanking>();
        var baselineTime = 1.0;

        // Find baseline if specified
        if (group.HasBaseline)
        {
            var baselineResult = results.FirstOrDefault(r =>
                r.TestInstanceContainer!.TestCaseId.TestCaseName.GetMethodPart().Equals(group.BaselineMethod, StringComparison.OrdinalIgnoreCase));

            if (baselineResult?.PerformanceTimerResults != null)
            {
                var executionTimes = baselineResult.PerformanceTimerResults.ExecutionIterationPerformances
                    .Select(x => x.GetDurationFromTicks().MilliSeconds.Duration)
                    .ToList();
                if (executionTimes.Any())
                {
                    baselineTime = executionTimes
                        .OrderBy(t => t)
                        .Skip(executionTimes.Count / 2)
                        .First();
                }
            }
        }

        // Calculate rankings based on median execution time
        var methodTimes = results
            .Where(r => r.PerformanceTimerResults != null && r.TestInstanceContainer != null)
            .Select(r => new
            {
                TestCaseId = r.TestInstanceContainer!.TestCaseId,
                MedianTime = CalculateMedianTime(r.PerformanceTimerResults!)
            })
            .Where(x => x.MedianTime > 0)
            .OrderBy(x => x.MedianTime)
            .ToList();

        for (int i = 0; i < methodTimes.Count; i++)
        {
            var methodTime = methodTimes[i];
            var relativePerformance = methodTime.MedianTime / baselineTime;

            rankings.Add(new MethodRanking(
                methodTime.TestCaseId,
                i + 1,
                methodTime.MedianTime,
                relativePerformance));
        }

        return rankings;
    }

    private static double CalculateMedianTime(PerformanceTimer performanceTimer)
    {
        var executionTimes = performanceTimer.ExecutionIterationPerformances
            .Select(x => x.GetDurationFromTicks().MilliSeconds.Duration)
            .ToList();

        if (!executionTimes.Any()) return 0;

        var sortedTimes = executionTimes.OrderBy(t => t).ToList();
        return sortedTimes.Skip(sortedTimes.Count / 2).First();
    }
}
