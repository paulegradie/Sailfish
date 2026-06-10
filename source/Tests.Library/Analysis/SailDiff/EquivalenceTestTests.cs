using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Formatting;
using Sailfish.Analysis.SailDiff.Statistics;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Analysis.SailDiff.Statistics.Tests.KolmogorovSmirnovTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.PermutationTest;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TTest;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

/// <summary>
/// TOST equivalence testing: the statistics themselves, the opt-in wiring through
/// StatisticalTestExecutor, and the formatter surfaces that distinguish "equivalent within the
/// margin" from "inconclusive".
/// </summary>
public class EquivalenceTestTests
{
    // ─── The statistic ─────────────────────────────────────────────────────────────────

    [Fact]
    public void IdenticalDistributions_AreEquivalentWithinFivePercent()
    {
        var before = TightSample(level: 100.0, n: 25);
        var after = TightSample(level: 100.5, n: 25); // 0.5% shift, well inside ±5%

        var result = EquivalenceTest.LogScaleTost(before, after, marginPercent: 5.0, alpha: 0.05);

        result.ShouldNotBeNull();
        result.IsEquivalent.ShouldBeTrue();
        result.PValue.ShouldBeLessThan(0.05);
        result.MarginPercent.ShouldBe(5.0);
    }

    [Fact]
    public void TenPercentShift_IsNotEquivalentWithinFivePercent()
    {
        var before = TightSample(level: 100.0, n: 25);
        var after = TightSample(level: 110.0, n: 25); // 10% shift, outside ±5%

        var result = EquivalenceTest.LogScaleTost(before, after, marginPercent: 5.0, alpha: 0.05);

        result.ShouldNotBeNull();
        result.IsEquivalent.ShouldBeFalse();
        // The "not slower than +5%" one-sided test should decisively fail to reject.
        result.PValueLower.ShouldBeGreaterThan(0.5);
    }

    [Fact]
    public void TenPercentShift_IsEquivalentWithinTwentyPercent()
    {
        // The same data, judged against a margin generous enough to contain the shift.
        var before = TightSample(level: 100.0, n: 25);
        var after = TightSample(level: 110.0, n: 25);

        var result = EquivalenceTest.LogScaleTost(before, after, marginPercent: 20.0, alpha: 0.05);

        result.ShouldNotBeNull();
        result.IsEquivalent.ShouldBeTrue();
    }

    [Fact]
    public void UnderpoweredRun_DoesNotClaimEquivalence()
    {
        // Same central tendency but tiny n and huge spread: the data cannot demonstrate the
        // ratio lies inside ±5% — exactly the "inconclusive" case TOST exists to expose.
        var before = new double[] { 50, 100, 150 };
        var after = new double[] { 60, 100, 140 };

        var result = EquivalenceTest.LogScaleTost(before, after, marginPercent: 5.0, alpha: 0.05);

        result.ShouldNotBeNull();
        result.IsEquivalent.ShouldBeFalse();
        result.PValue.ShouldBeGreaterThan(0.05);
    }

    [Fact]
    public void TostPValue_IsTheLargerOneSidedPValue()
    {
        var before = TightSample(level: 100.0, n: 20);
        var after = TightSample(level: 102.0, n: 20);

        var result = EquivalenceTest.LogScaleTost(before, after, marginPercent: 5.0, alpha: 0.05);

        result.ShouldNotBeNull();
        result.PValue.ShouldBe(Math.Max(result.PValueLower, result.PValueUpper));
    }

    [Fact]
    public void DegenerateInputs_Abstain()
    {
        var fine = TightSample(level: 100.0, n: 10);

        // Non-positive values can't be log-transformed.
        EquivalenceTest.LogScaleTost(new[] { -1.0, 2, 3 }, fine, 5.0, 0.05).ShouldBeNull();
        EquivalenceTest.LogScaleTost(fine, new[] { 0.0, 2, 3 }, 5.0, 0.05).ShouldBeNull();

        // Too few observations.
        EquivalenceTest.LogScaleTost(new[] { 1.0 }, fine, 5.0, 0.05).ShouldBeNull();

        // Zero variance on both sides leaves no standard error to test against.
        EquivalenceTest.LogScaleTost(new[] { 100.0, 100, 100 }, new[] { 100.0, 100, 100 }, 5.0, 0.05).ShouldBeNull();

        // Invalid margin / alpha.
        EquivalenceTest.LogScaleTost(fine, fine, 0.0, 0.05).ShouldBeNull();
        EquivalenceTest.LogScaleTost(fine, fine, -5.0, 0.05).ShouldBeNull();
        EquivalenceTest.LogScaleTost(fine, fine, 5.0, 0.0).ShouldBeNull();
    }

    // ─── Settings ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Settings_EquivalenceMargin_DefaultsOff_AndValidates()
    {
        var settings = new SailDiffSettings();
        settings.EquivalenceMarginPercent.ShouldBeNull();

        settings.SetEquivalenceMarginPercent(5.0);
        settings.EquivalenceMarginPercent.ShouldBe(5.0);

        settings.SetEquivalenceMarginPercent(null);
        settings.EquivalenceMarginPercent.ShouldBeNull();

        Should.Throw<ArgumentOutOfRangeException>(() => settings.SetEquivalenceMarginPercent(0));
        Should.Throw<ArgumentOutOfRangeException>(() => settings.SetEquivalenceMarginPercent(-3));
        Should.Throw<ArgumentOutOfRangeException>(() => settings.SetEquivalenceMarginPercent(double.NaN));
    }

    // ─── Executor wiring ───────────────────────────────────────────────────────────────

    [Fact]
    public void Executor_AttachesEquivalence_WhenMarginConfigured()
    {
        var executor = BuildRealExecutor();
        var settings = new SailDiffSettings(useOutlierDetection: false);
        settings.SetEquivalenceMarginPercent(5.0);

        var before = TightSample(level: 100.0, n: 25);
        var after = TightSample(level: 100.5, n: 25);

        var result = executor.ExecuteStatisticalTest(before, after, settings);

        var equivalence = result.StatisticalTestResult.Equivalence;
        equivalence.ShouldNotBeNull();
        equivalence.IsEquivalent.ShouldBeTrue();
        equivalence.MarginPercent.ShouldBe(5.0);
    }

    [Fact]
    public void Executor_LeavesEquivalenceNull_ByDefault()
    {
        var executor = BuildRealExecutor();
        var settings = new SailDiffSettings(useOutlierDetection: false);

        var before = TightSample(level: 100.0, n: 25);
        var after = TightSample(level: 100.5, n: 25);

        var result = executor.ExecuteStatisticalTest(before, after, settings);

        result.StatisticalTestResult.Equivalence.ShouldBeNull();
    }

    [Fact]
    public void Executor_RunsEquivalence_ForEveryTestType()
    {
        // The TOST is a supplement computed at the executor chokepoint — it must appear no matter
        // which significance test the user selected.
        var before = TightSample(level: 100.0, n: 25);
        var after = TightSample(level: 100.5, n: 25);

        foreach (var testType in new[]
                 {
                     TestType.Test, TestType.WilcoxonRankSumTest, TestType.KolmogorovSmirnovTest,
                     TestType.TwoSampleWilcoxonSignedRankTest, TestType.PermutationTest
                 })
        {
            var executor = BuildRealExecutor();
            var settings = new SailDiffSettings(useOutlierDetection: false, testType: testType);
            settings.SetEquivalenceMarginPercent(5.0);

            var result = executor.ExecuteStatisticalTest(before, after, settings);

            result.StatisticalTestResult.Equivalence.ShouldNotBeNull($"equivalence missing for {testType}");
        }
    }

    // ─── Formatter surfaces ────────────────────────────────────────────────────────────

    [Fact]
    public void ImpactSummary_NotSignificantAndEquivalent_SaysEquivalentWithinMargin()
    {
        var data = BuildComparisonData(pValue: 0.6, changeDescription: "No Change");
        data.Statistics.Equivalence = new EquivalenceTestResult(5.0, 0.001, 0.002, 0.002, IsEquivalent: true);

        var output = new ImpactSummaryFormatter().CreateImpactSummary(data, OutputContext.Console);

        output.ShouldContain("NOT SIGNIFICANT");
        output.ShouldContain("equivalent within ±5%");
    }

    [Fact]
    public void ImpactSummary_NotSignificantWithoutPower_SaysInconclusive()
    {
        var data = BuildComparisonData(pValue: 0.6, changeDescription: "No Change");
        data.Statistics.Equivalence = new EquivalenceTestResult(5.0, 0.4, 0.3, 0.4, IsEquivalent: false);

        var output = new ImpactSummaryFormatter().CreateImpactSummary(data, OutputContext.Console);

        output.ShouldContain("NOT SIGNIFICANT");
        output.ShouldContain("inconclusive at ±5% margin");
    }

    [Fact]
    public void ImpactSummary_SignificantResult_DoesNotMentionEquivalence()
    {
        var data = BuildComparisonData(pValue: 0.001, changeDescription: "Regressed");
        data.Statistics.Equivalence = new EquivalenceTestResult(5.0, 0.9, 0.001, 0.9, IsEquivalent: false);

        var output = new ImpactSummaryFormatter().CreateImpactSummary(data, OutputContext.Console);

        output.ShouldNotContain("inconclusive");
        output.ShouldNotContain("equivalent within");
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Deterministic low-variance sample around <paramref name="level"/> (~±2% spread).
    /// </summary>
    private static double[] TightSample(double level, int n)
    {
        return Enumerable.Range(0, n)
            .Select(i => level * (1.0 + 0.02 * Math.Sin(i * 1.7)))
            .ToArray();
    }

    private static StatisticalTestExecutor BuildRealExecutor()
    {
        var preprocessor = new TestPreprocessor(new SailfishOutlierDetector());
        return new StatisticalTestExecutor(
            new MannWhitneyWilcoxonTest(preprocessor),
            new Test(preprocessor),
            new TwoSampleWilcoxonSignedRankTest(preprocessor),
            new KolmogorovSmirnovTest(preprocessor),
            new PermutationTest(preprocessor));
    }

    private static SailDiffComparisonData BuildComparisonData(double pValue, string changeDescription)
    {
        return new SailDiffComparisonData
        {
            GroupName = "TestGroup",
            PrimaryMethodName = "Before",
            ComparedMethodName = "After",
            Statistics = new StatisticalTestResult(
                meanBefore: 100.0,
                meanAfter: 101.0,
                medianBefore: 100.0,
                medianAfter: 101.0,
                testStatistic: 1.0,
                pValue: pValue,
                changeDescription: changeDescription,
                sampleSizeBefore: 25,
                sampleSizeAfter: 25,
                rawDataBefore: TightSample(100.0, 25),
                rawDataAfter: TightSample(101.0, 25),
                additionalResults: new Dictionary<string, object>()),
            Metadata = new ComparisonMetadata
            {
                SampleSize = 25,
                AlphaLevel = 0.05,
                TestType = "T-Test",
                OutliersRemoved = 0
            },
            IsPerspectiveBased = false
        };
    }
}
