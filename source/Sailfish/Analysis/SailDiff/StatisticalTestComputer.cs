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
        // StatisticalTestResult, and the verdict (ChangeDescription) is re-gated on it below
        // so the headline a user reads and the printed q-value always agree.
        ApplyBenjaminiHochberg(results, settings.Alpha);

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

    private static void ApplyBenjaminiHochberg(ConcurrentBag<SailDiffResult> results, double alpha)
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

        // No correction needed for fewer than two comparisons — q == p, and the wrapper's
        // raw-p verdict is already the q-gated verdict.
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
            {
                stat.QValue = q;
                DemoteVerdictWhenNotSignificantAfterFdr(stat, q, alpha);
            }
        }
    }

    /// <summary>
    /// Re-gates the headline verdict on the BH-adjusted q-value. Each test wrapper decides
    /// Improved/Regressed from the raw per-pair p-value — correct in isolation, but across a family
    /// of comparisons it is exactly the multiplicity problem BH exists to control, and it let a row
    /// read "Regressed" while its own printed q-value said not significant. Since q ≥ p, FDR control
    /// can only <em>demote</em> a verdict (significant → not significant), never promote one, so the
    /// direction never needs re-deriving — a NoChange verdict stays NoChange.
    /// </summary>
    private static void DemoteVerdictWhenNotSignificantAfterFdr(StatisticalTestResult stat, double q, double alpha)
    {
        if (stat.ChangeDescription == SailfishChangeDirection.NoChange) return;
        if (SailDiffSignificance.IsSignificantPositive(q, alpha)) return;
        stat.ChangeDescription = SailfishChangeDirection.NoChange;
    }
}