using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TTest;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using Tests.Common.Builders;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

/// <summary>
/// Integration tests for the Tier 2 completeness work — effect sizes, difference CIs,
/// BH-FDR across pairs, log-transform. Each test exercises a public surface so a downstream
/// regression breaks here before it leaks into formatter or report output.
/// </summary>
public class Tier2CompleteTests
{
    // ─── Effect size & difference CI — t-test path ─────────────────────────────────────

    [Fact]
    public void TTest_PopulatesEffectSizeAndDifference()
    {
        var rng = new Random(7);
        var before = Enumerable.Range(0, 30).Select(_ => 100 + rng.NextDouble() * 5).ToArray();
        var after = Enumerable.Range(0, 30).Select(_ => 110 + rng.NextDouble() * 5).ToArray();

        var test = new Test(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(before, after,
            new SailDiffSettings(alpha: 0.05, useOutlierDetection: false, testType: TestType.Test));

        var stat = result.StatisticalTestResult;
        stat.EffectSize.ShouldNotBeNull();
        stat.EffectSize!.Name.ShouldBe("Hedges' g");
        // Substantial effect (≈ 2σ shift), g should be clearly positive.
        stat.EffectSize.Value.ShouldBeGreaterThan(1.0);

        stat.Difference.ShouldNotBeNull();
        stat.Difference!.Name.ShouldBe("Mean difference");
        stat.Difference.Value.ShouldBe(10.0, tolerance: 1.0);
        stat.Difference.Units.ShouldBe("ms");
    }

    // ─── Effect size & difference CI — rank-sum path ───────────────────────────────────

    [Fact]
    public void MannWhitney_PopulatesEffectSizeAndDifference()
    {
        var rng = new Random(7);
        var before = Enumerable.Range(0, 30).Select(_ => 100 + rng.NextDouble() * 5).ToArray();
        var after = Enumerable.Range(0, 30).Select(_ => 110 + rng.NextDouble() * 5).ToArray();

        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(before, after,
            new SailDiffSettings(alpha: 0.05, useOutlierDetection: false));

        var stat = result.StatisticalTestResult;
        stat.EffectSize.ShouldNotBeNull();
        stat.EffectSize!.Name.ShouldBe("Cliff's delta");
        stat.EffectSize.Value.ShouldBeInRange(-1.0, 1.0);
        // After is clearly larger than Before → positive Cliff's delta.
        stat.EffectSize.Value.ShouldBeGreaterThan(0.5);

        stat.Difference.ShouldNotBeNull();
        stat.Difference!.Name.ShouldBe("Hodges-Lehmann shift");
        // The HL estimate should recover the ~10 ms shift between the two samples.
        stat.Difference.Value.ShouldBe(10.0, tolerance: 2.0);
    }

    // ─── BH-FDR across the family of comparisons ───────────────────────────────────────

    [Fact]
    public void StatisticalTestComputer_AppliesBenjaminiHochbergAcrossPairs()
    {
        // Build 10 mock test cases. The mock executor returns p-values in a linear pattern
        // (0.005, 0.015, …, 0.095). BH should leave the smallest unchanged and inflate the
        // largest the most. Specifically for sorted p-values p_i and m = 10:
        //   q_i = m / (i+1) · p_i  (clipped, monotonised)
        //   q_0 = 10/1 · 0.005 = 0.050
        //   q_9 = 10/10 · 0.095 = 0.095
        // The middle inflation factor reaches up to 10× for the smallest p.
        var executor = Substitute.For<IStatisticalTestExecutor>();
        var aggregator = Substitute.For<IPerformanceRunResultAggregator>();
        var pValueByIndex = new Dictionary<string, double>();

        var beforeResults = new List<PerformanceRunResult>();
        var afterResults = new List<PerformanceRunResult>();
        for (var i = 0; i < 10; i++)
        {
            var name = $"TestClass.Method{i:D2}";
            var rawP = 0.005 + i * 0.010;
            pValueByIndex[name] = rawP;
            beforeResults.Add(MakePerf(name));
            afterResults.Add(MakePerf(name));
        }

        aggregator
            .Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(x => AggregatedPerformanceResult.Aggregate((TestCaseId)x[0], [MakePerf(((TestCaseId)x[0]).DisplayName)]));

        executor
            .ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(_ =>
            {
                // We don't know which test case this is from the args, so return distinct
                // results in order. Capture below sorts the actual SailDiff results by name
                // to align them with the expected p-values.
                return BuildStatResult(0.0);
            });

        // Use a custom returns function that varies by call count.
        var callIndex = 0;
        executor
            .ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(_ =>
            {
                var idx = callIndex++;
                var name = $"TestClass.Method{idx:D2}";
                return BuildStatResult(pValueByIndex[name]);
            });

        var computer = new StatisticalTestComputer(executor, aggregator);
        var settings = new SailDiffSettings(alpha: 0.05, useOutlierDetection: false);

        var beforeData = new TestData(beforeResults.Select(b => b.DisplayName), beforeResults);
        var afterData = new TestData(afterResults.Select(a => a.DisplayName), afterResults);

        var results = computer.ComputeTest(beforeData, afterData, settings);

        // Every comparison should now carry a q-value.
        foreach (var r in results)
        {
            var stat = r.TestResultsWithOutlierAnalysis.StatisticalTestResult;
            stat.QValue.ShouldNotBeNull(customMessage: $"q missing for {r.TestCaseId.DisplayName}");
        }

        // q-values should be monotonically non-decreasing in p-value order. Take the results
        // sorted by their actual p-value and check q is non-decreasing too.
        var sorted = results
            .Select(r => r.TestResultsWithOutlierAnalysis.StatisticalTestResult)
            .OrderBy(s => s.PValue)
            .ToList();
        for (var i = 1; i < sorted.Count; i++)
            sorted[i].QValue!.Value.ShouldBeGreaterThanOrEqualTo(sorted[i - 1].QValue!.Value,
                customMessage: "BH q-values must be non-decreasing with p-value rank");

        // At least the smallest p must inflate. p_min = 0.005, q_min = 10/1 · 0.005 = 0.050.
        sorted[0].QValue!.Value.ShouldBe(0.050, tolerance: 1e-9);
    }

    [Fact]
    public void StatisticalTestComputer_SingleComparison_QEqualsP()
    {
        // No correction is meaningful for a family of one — q should equal p.
        var executor = Substitute.For<IStatisticalTestExecutor>();
        var aggregator = Substitute.For<IPerformanceRunResultAggregator>();

        var pr = MakePerf("TestClass.Solo");
        var beforeData = new TestData([pr.DisplayName], [pr]);
        var afterData = new TestData([pr.DisplayName], [pr]);

        aggregator.Aggregate(Arg.Any<TestCaseId>(), Arg.Any<IReadOnlyCollection<PerformanceRunResult>>())
            .Returns(x => AggregatedPerformanceResult.Aggregate((TestCaseId)x[0], [pr]));
        executor.ExecuteStatisticalTest(Arg.Any<double[]>(), Arg.Any<double[]>(), Arg.Any<SailDiffSettings>())
            .Returns(BuildStatResult(0.023));

        var computer = new StatisticalTestComputer(executor, aggregator);
        var results = computer.ComputeTest(beforeData, afterData, new SailDiffSettings());

        var stat = results.Single().TestResultsWithOutlierAnalysis.StatisticalTestResult;
        stat.QValue.ShouldNotBeNull();
        stat.QValue!.Value.ShouldBe(0.023, 1e-12);
    }

    // ─── Log-transform option ─────────────────────────────────────────────────────────

    [Fact]
    public void TTest_LogTransform_ChangesDifferenceReportToRatio()
    {
        // 1.10× shift (10% slower) — a multiplicative effect that log-transform should
        // recover as ratio ≈ 1.10 in the DifferenceReport.
        var rng = new Random(7);
        var before = Enumerable.Range(0, 50).Select(_ => 100.0 * (1 + 0.02 * rng.NextDouble())).ToArray();
        var after = Enumerable.Range(0, 50).Select(_ => 110.0 * (1 + 0.02 * rng.NextDouble())).ToArray();

        var test = new Test(new TestPreprocessor(new SailfishOutlierDetector()));
        var resultLinear = test.ExecuteTest(before, after,
            new SailDiffSettings(alpha: 0.05, useOutlierDetection: false, testType: TestType.Test, logTransform: false));
        var resultLog = test.ExecuteTest(before, after,
            new SailDiffSettings(alpha: 0.05, useOutlierDetection: false, testType: TestType.Test, logTransform: true));

        resultLinear.StatisticalTestResult.Difference!.Name.ShouldBe("Mean difference");
        resultLinear.StatisticalTestResult.Difference.Units.ShouldBe("ms");

        resultLog.StatisticalTestResult.Difference!.Name.ShouldContain("ratio");
        resultLog.StatisticalTestResult.Difference.Units.ShouldBe("× ratio");
        resultLog.StatisticalTestResult.Difference.Value.ShouldBe(1.10, tolerance: 0.05);
    }

    [Fact]
    public void TTest_LogTransform_OnNonPositiveData_FallsBackToRawScale()
    {
        // Log isn't defined for ≤ 0 — the wrapper must gracefully fall through to raw-scale
        // testing rather than throw or emit NaNs.
        var before = new double[] { 1.0, 2.0, -3.0, 4.0, 5.0 };
        var after = new double[] { 1.5, 2.5, 0.0, 4.5, 5.5 };

        var test = new Test(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(before, after,
            new SailDiffSettings(alpha: 0.05, useOutlierDetection: false, testType: TestType.Test, logTransform: true));

        result.ExceptionMessage.ShouldBeEmpty();
        // Falls back to raw scale → Difference report uses Mean difference / ms units.
        result.StatisticalTestResult.Difference!.Name.ShouldBe("Mean difference");
        result.StatisticalTestResult.Difference.Units.ShouldBe("ms");
    }

    // ─── Critical regression: MannWhitney must reject when n1 > n2 ───────────────────────
    //
    // The exact-null CCDF lookup branched on (n1 ≤ n2) and returned the *CDF* when n1 > n2.
    // Combined with the wrapper's two-sided p-value formula, p collapsed to ≈ 2·F(U2) and the
    // test silently failed to reject even maximally-separated data. The fix removes the branch
    // and uses the discrete CCDF directly; see MannWhitneyDistribution.cs for the rationale.

    [Fact]
    public void MannWhitney_NLargerThanM_MaximallySeparatedSamples_RejectsAtAlpha005()
    {
        // 5-vs-4 maximally-separated samples. Pre-fix: p = 1, post-fix: p ≈ 2/126 ≈ 0.016.
        // Using outlier-detection off because Tukey on tiny tight clusters can remove
        // everything and we want to pin the raw-input behavior.
        var before = new double[] { 1, 2, 3, 4, 5 };
        var after = new double[] { 6, 7, 8, 9 };

        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(before, after,
            new SailDiffSettings(alpha: 0.05, useOutlierDetection: false));

        result.ExceptionMessage.ShouldBeEmpty();
        var stat = result.StatisticalTestResult;
        stat.PValue.ShouldBeLessThan(0.05,
            customMessage: $"5-vs-4 maximally-separated samples must reject at α=0.05; got p={stat.PValue}");
        stat.ChangeDescription.ShouldBe(SailfishChangeDirection.Regressed,
            customMessage: "After is uniformly larger than Before, so the wrapper should report Regressed.");
    }

    [Fact]
    public void MannWhitney_NLargerThanM_AndNSmallerThanM_GivesConsistentPValue()
    {
        // Symmetry check: swapping the two samples must flip the direction label but yield
        // an identical p-value. Pre-fix this would diverge by orders of magnitude because
        // only one orientation went through the buggy branch.
        var a = new double[] { 1, 2, 3, 4, 5 };
        var b = new double[] { 6, 7, 8, 9 };

        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        var settings = new SailDiffSettings(alpha: 0.05, useOutlierDetection: false);

        var ab = test.ExecuteTest(a, b, settings);
        var ba = test.ExecuteTest(b, a, settings);

        ab.ExceptionMessage.ShouldBeEmpty();
        ba.ExceptionMessage.ShouldBeEmpty();
        ab.StatisticalTestResult.PValue.ShouldBe(ba.StatisticalTestResult.PValue, tolerance: 1e-12,
            customMessage: "p-value must be invariant under sample-order swap.");
        ab.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.Regressed);
        ba.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.Improved.ToString());
    }

    // ─── Critical regression: NaN p-value cannot contaminate BH-FDR family ──────────────
    //
    // ClampP(NaN) used to map to 0.0, placing the NaN pair at the head of the sorted-ascending
    // vector and dragging every other pair's q-value down via the right-to-left running min.
    // The fix maps NaN to 1.0 (most conservative), so a single failed test cannot fabricate
    // significance elsewhere in the family.

    [Fact]
    public void BenjaminiHochberg_NaNPValuesDoNotContaminateOtherPairsQValues()
    {
        // 4 well-behaved pairs at p = {0.20, 0.30, 0.40, 0.50} — none significant at α = 0.05.
        // Pre-fix, adding a single NaN pair would compute m = 5, sort [NaN→0, 0.20, 0.30, 0.40, 0.50]
        // and (with monotonisation) push q-values down enough that ε-shifted alphas could flag
        // the 0.20 pair as significant. Post-fix, NaN sorts to the end as p = 1 and is harmless.
        var withoutNaN = new Dictionary<string, double>
        {
            ["A"] = 0.20,
            ["B"] = 0.30,
            ["C"] = 0.40,
            ["D"] = 0.50,
        };
        var withNaN = new Dictionary<string, double>(withoutNaN)
        {
            ["E"] = double.NaN,
        };

        var qWithout = MultipleComparisons.BenjaminiHochbergAdjust(withoutNaN);
        var qWith = MultipleComparisons.BenjaminiHochbergAdjust(withNaN);

        // q-values for the well-behaved pairs must be *no smaller* with the NaN present.
        // (They will typically be slightly larger because m grew from 4 to 5.)
        foreach (var key in withoutNaN.Keys)
        {
            qWith[key].ShouldBeGreaterThanOrEqualTo(qWithout[key] - 1e-12,
                customMessage: $"Adding a NaN pair lowered the q-value for {key} from {qWithout[key]} to {qWith[key]} — the family was contaminated.");
        }

        // The NaN entry itself should land at q = 1.0 (the conservative cap).
        qWith["E"].ShouldBe(1.0, tolerance: 1e-12,
            customMessage: "NaN p-value should be treated as the most conservative; q must be 1.0.");
    }

    [Fact]
    public void BenjaminiHochberg_OnlyNaNPValues_AllQuotedAsOne()
    {
        var all = new Dictionary<string, double>
        {
            ["A"] = double.NaN,
            ["B"] = double.NaN,
        };

        var q = MultipleComparisons.BenjaminiHochbergAdjust(all);

        q["A"].ShouldBe(1.0, tolerance: 1e-12);
        q["B"].ShouldBe(1.0, tolerance: 1e-12);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────────────

    private static TestResultWithOutlierAnalysis BuildStatResult(double pValue)
    {
        var stat = new StatisticalTestResult(
            meanBefore: 5.0,
            meanAfter: 6.0,
            medianBefore: 5.0,
            medianAfter: 6.0,
            testStatistic: 2.0,
            pValue: pValue,
            changeDescription: "NoChange",
            sampleSizeBefore: 5,
            sampleSizeAfter: 5,
            rawDataBefore: [1.0, 2, 3, 4, 5],
            rawDataAfter: [2.0, 3, 4, 5, 6],
            additionalResults: new Dictionary<string, object>());
        return new TestResultWithOutlierAnalysis(stat, null, null);
    }

    private static PerformanceRunResult MakePerf(string displayName)
    {
        return PerformanceRunResultBuilder.Create()
            .WithDisplayName(displayName)
            .WithRawExecutionResults([1.0, 2.0, 3.0, 4.0, 5.0])
            .WithSampleSize(5)
            .WithMean(3.0)
            .WithMedian(3.0)
            .WithStdDev(1.0)
            .WithVariance(1.0)
            .WithNumWarmupIterations(3)
            .WithDataWithOutliersRemoved([1.0, 2.0, 3.0, 4.0, 5.0])
            .WithUpperOutliers([])
            .WithLowerOutliers([])
            .WithTotalNumOutliers(0)
            .Build();
    }
}
