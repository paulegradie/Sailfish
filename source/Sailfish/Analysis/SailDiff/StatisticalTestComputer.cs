using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sailfish.Analysis.SailDiff.Statistics;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Methods;
using System.Collections.Concurrent;

namespace Sailfish.Analysis.SailDiff;

public interface IStatisticalTestComputer
{
    List<SailDiffResult> ComputeTest(TestData beforeTestData, TestData afterTestData, SailDiffSettings settings);
}

public class StatisticalTestComputer : IStatisticalTestComputer
{
    private readonly IPerformanceRunResultAggregator _aggregator;
    private readonly IStatisticalTestExecutor _statisticalTestExecutor;

    public StatisticalTestComputer(IStatisticalTestExecutor statisticalTestExecutor, IPerformanceRunResultAggregator aggregator)
    {
        _aggregator = aggregator;
        _statisticalTestExecutor = statisticalTestExecutor;
    }

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
                var afterCompiled = _aggregator.Aggregate(
                    testCaseId,
                    after
                        .Data
                        .Where(x => new TestCaseId(x.DisplayName).Equals(testCaseId))
                        .ToList());

                var beforeCompiled = _aggregator.Aggregate(
                    testCaseId,
                    before
                        .Data
                        .Where(x => new TestCaseId(x.DisplayName).Equals(testCaseId))
                        .ToList());

                if (beforeCompiled is null || afterCompiled is null) return;

                if (beforeCompiled.AggregatedRawExecutionResults.Length < 3 || afterCompiled.AggregatedRawExecutionResults.Length < 3) return;

                var result = _statisticalTestExecutor.ExecuteStatisticalTest(
                    beforeCompiled.AggregatedRawExecutionResults,
                    afterCompiled.AggregatedRawExecutionResults,
                    settings);

                results.Add(new SailDiffResult(testCaseId, result));
            });

        // Apply Benjamini-Hochberg FDR control across the family of comparisons. Pre-Tier-2,
        // each pair was evaluated at α independently — running 100 comparisons at α=0.05
        // expects ~5 false positives just by chance. The q-value lives on each result's
        // StatisticalTestResult so downstream formatters can prefer it over the raw p-value
        // when judging significance.
        ApplyBenjaminiHochberg(results);

        // Honour the user's explicit opt-out. The previous code also silently skipped
        // ordering when results.Count > 60 — an undocumented threshold that produced
        // different output ordering for large workloads with no warning. Removed; sorting
        // a few hundred SailDiffResult entries is negligible compared to the test runs
        // that produced them.
        if (settings.DisableOrdering) return [.. results];

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

    private static void ApplyBenjaminiHochberg(ConcurrentBag<SailDiffResult> results)
    {
        var pValues = new Dictionary<string, double>();
        foreach (var r in results)
        {
            var stat = r.TestResultsWithOutlierAnalysis?.StatisticalTestResult;
            // Skip failed tests and any malformed p-values — they shouldn't influence the
            // BH ranking of the surviving comparisons.
            if (stat is null || stat.Failed) continue;
            if (double.IsNaN(stat.PValue)) continue;
            pValues[r.TestCaseId.DisplayName] = stat.PValue;
        }

        // No correction needed for fewer than two comparisons — q == p.
        if (pValues.Count < 2)
        {
            foreach (var r in results)
            {
                var stat = r.TestResultsWithOutlierAnalysis?.StatisticalTestResult;
                if (stat is null || stat.Failed || double.IsNaN(stat.PValue)) continue;
                stat.QValue = stat.PValue;
            }
            return;
        }

        var qValues = MultipleComparisons.BenjaminiHochbergAdjust(pValues);
        foreach (var r in results)
        {
            var stat = r.TestResultsWithOutlierAnalysis?.StatisticalTestResult;
            if (stat is null) continue;
            if (qValues.TryGetValue(r.TestCaseId.DisplayName, out var q))
                stat.QValue = q;
        }
    }
}