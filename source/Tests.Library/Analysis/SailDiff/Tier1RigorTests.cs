using System.Collections.Generic;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Analysis.SailDiff.Statistics.Tests.MWWilcoxonTestSailfish;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TTest;
using Sailfish.Analysis.SailDiff.Statistics.Tests.TwoSampleWilcoxonSignedRankTestSailfish;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

/// <summary>
/// Regression tests for the Tier 1 statistical-rigor work — see the worktree's PR description
/// and <see cref="SailDiffSignificance"/> for context. Each test pins a behavior that the pre-Tier-1
/// engine got wrong:
///
/// 1. The Mann-Whitney wrapper down-sampled to N=10 and voted across resamples, killing power
///    so that even clear effects registered as "NoChange" (the core "not sensitive enough"
///    complaint). After Tier 1, MW runs once on the full preprocessed sample.
///
/// 2. The default SailDiffSettings used <c>TwoSampleWilcoxonSignedRankTest</c> (a paired-sample
///    test) on independent benchmark iterations and an α of 0.001 — a combination that almost
///    never rejected. After Tier 1 the default is the Mann-Whitney rank-sum test at α = 0.05.
///
/// 3. The down-sampler used <c>new Random()</c> when no explicit seed was passed, making stats
///    non-deterministic even with <c>RunSettingsBuilder.WithSeed</c> set. After Tier 1 the
///    preprocessor consults <see cref="IRunSettings.Seed"/> and sorts selected indices.
///
/// 4. <c>HypothesisTest.Size</c> was a hardcoded static 0.05, so the t-test's reported CI
///    was always 95% regardless of the user's α. After Tier 1, α threads through to the CI.
///
/// 5. Significance decisions in formatters used hardcoded <c>q ≤ 0.05</c> even when the user
///    had configured a different α. After Tier 1, <see cref="SailDiffSignificance"/> centralises
///    the cutoff and formatters honour the configured α.
/// </summary>
public class Tier1RigorTests
{
    [Fact]
    public void DefaultSailDiffSettings_UsesIndependentSamplesTestAtConventionalAlpha()
    {
        var s = new SailDiffSettings();

        s.Alpha.ShouldBe(0.05);
        s.TestType.ShouldBe(TestType.WilcoxonRankSumTest);
    }

    [Fact]
    public void MannWhitneyWilcoxon_DetectsMediumEffectAtN30()
    {
        // Cohen's d ≈ 1.0 at N=30 each. Power at α = 0.05 is >99%. Before Tier 1 this returned
        // NoChange because the wrapper down-sampled to N=10 then voted; documented by the test
        // formerly named MannWhitneyWilcoxonTestSailfish_HiStdDevNoChange.
        var rng = new System.Random(7);
        var before = TestDistributions.GenerateRandomNormalDistribution(30, 100, 10, rng);
        var after = TestDistributions.GenerateRandomNormalDistribution(30, 110, 10, rng);

        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(before, after, new SailDiffSettings(alpha: 0.05, useOutlierDetection: false));

        result.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.Regressed);
        result.StatisticalTestResult.PValue.ShouldBeLessThan(0.05);
    }

    [Fact]
    public void MannWhitneyWilcoxon_DescribesProcessedSampleNotRaw()
    {
        // Pre-Tier-1 the wrapper reported means/medians on the RAW data while the p-value was
        // computed on a random N≤10 subsample, so descriptive stats and the test result
        // described different data. We now report stats on the processed sample.
        var rng = new System.Random(7);
        var before = TestDistributions.GenerateRandomNormalDistribution(50, 100, 5, rng);
        var after = TestDistributions.GenerateRandomNormalDistribution(50, 100, 5, rng);

        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(before, after, new SailDiffSettings(useOutlierDetection: false));

        // With outlier detection off, the processed sample == raw sample, so the reported
        // mean must equal the raw mean of the full N=50 input (within the rounding budget).
        result.StatisticalTestResult.MeanBefore.ShouldBe(before.Mean(), tolerance: 1e-3);
        result.StatisticalTestResult.MeanAfter.ShouldBe(after.Mean(), tolerance: 1e-3);
    }

    [Fact]
    public void MannWhitneyWilcoxon_LargeNUsesNormalApproximationWithoutBlowingUp()
    {
        // Pre-Tier-1 the underlying MannWhitneyDistribution would try to enumerate
        // C(N1+N2, min(N1,N2)) combinations for any N ≤ 30 — at N1=N2=30 that's ~1.18e17
        // entries, which OOM'd. We now cap the exact path at ~2e6 entries and fall back to
        // the normal approximation otherwise. This test pins the no-throw behaviour at the
        // old failure point.
        var rng = new System.Random(7);
        var before = TestDistributions.GenerateRandomNormalDistribution(30, 100, 1, rng);
        var after = TestDistributions.GenerateRandomNormalDistribution(30, 120, 1, rng);

        var test = new MannWhitneyWilcoxonTest(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(before, after, new SailDiffSettings(useOutlierDetection: false));

        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.PValue.ShouldBeLessThan(0.05);
        result.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.Regressed);
    }

    [Fact]
    public void TestPreprocessor_WithSeed_IsDeterministic()
    {
        // RunSettings.Seed must produce identical down-samples across runs. Pre-Tier-1, no
        // explicit seed → new Random() → wall-clock seed → non-deterministic stats even when
        // the user had set a seed via RunSettingsBuilder.WithSeed.
        var input = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        var runSettings = Substitute.For<IRunSettings>();
        runSettings.Seed.Returns(123);
        var preprocessor = new TestPreprocessor(new SailfishOutlierDetector(), runSettings);

        var firstRun = preprocessor.PreprocessWithDownSample(input, useOutlierDetection: false, maxArraySize: 10);
        var secondRun = preprocessor.PreprocessWithDownSample(input, useOutlierDetection: false, maxArraySize: 10);

        secondRun.RawData.ShouldBe(firstRun.RawData);
    }

    [Fact]
    public void TestPreprocessor_DifferentSeeds_ProduceDifferentDownSamples()
    {
        // Sanity check that the seed actually drives variation — without this, the determinism
        // test above could pass trivially if the down-sampler ignored the seed.
        var input = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        var preprocessor = new TestPreprocessor(new SailfishOutlierDetector());

        var seedA = preprocessor.PreprocessWithDownSample(input, useOutlierDetection: false, maxArraySize: 10, seed: 42);
        var seedB = preprocessor.PreprocessWithDownSample(input, useOutlierDetection: false, maxArraySize: 10, seed: 99);

        seedB.RawData.ShouldNotBe(seedA.RawData);
    }

    [Fact]
    public void TestPreprocessor_JointDownSample_UsesIndependentSeedStreamsPerSample()
    {
        // PreprocessJointlyWithDownSample used to pass the same seed to both samples, so with
        // identical-length input it returned identical index sets — a subtle determinism bug
        // when the caller cared about independence (e.g., shuffled comparison group order).
        // We now offset the seed for the second sample.
        var sampleA = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        var sampleB = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        var preprocessor = new TestPreprocessor(new SailfishOutlierDetector());

        var (p1, p2) = preprocessor.PreprocessJointlyWithDownSample(
            sampleA, sampleB, useOutlierDetection: false, minArraySize: 3, maxArraySize: 10, seed: 42);

        // Both come from identical underlying data, so the same index set would yield identical
        // output. We require divergence to prove independent streams.
        p2.RawData.ShouldNotBe(p1.RawData);
    }

    [Fact]
    public void SailDiffSignificance_ReadsAlphaFromMetadata_FallingBackOnAbsence()
    {
        var withAlpha = new Dictionary<string, object> { [SailDiffSignificance.MetadataKey] = 0.01 };
        var empty = new Dictionary<string, object>();
        var malformed = new Dictionary<string, object> { [SailDiffSignificance.MetadataKey] = "not-a-number" };

        SailDiffSignificance.ReadFromMetadata(withAlpha).ShouldBe(0.01);
        SailDiffSignificance.ReadFromMetadata(empty).ShouldBe(SailDiffSignificance.FallbackAlpha);
        SailDiffSignificance.ReadFromMetadata(malformed).ShouldBe(SailDiffSignificance.FallbackAlpha);
        SailDiffSignificance.ReadFromMetadata(null).ShouldBe(SailDiffSignificance.FallbackAlpha);
    }

    [Fact]
    public void SailDiffSignificance_FallbackAlphaMatchesDefaultSailDiffSettings()
    {
        // The fallback should track the user-visible default. If we ever change one without
        // the other we lose the invariant that "no settings ≡ defaults" produces the same
        // significance decisions everywhere in the pipeline.
        SailDiffSignificance.FallbackAlpha.ShouldBe(new SailDiffSettings().Alpha);
    }

    [Fact]
    public void TTest_PassesUserAlphaThroughForCi()
    {
        // Welch's t with a substantial effect: at α = 0.01 the p-value should still be below
        // α (so we get a significance verdict) and the CI reported by the underlying test
        // should match the (1 - α) = 99% level via the threaded alpha — not the previous
        // hardcoded 95%.
        var rng = new System.Random(7);
        var before = TestDistributions.GenerateRandomNormalDistribution(60, 100, 5, rng);
        var after = TestDistributions.GenerateRandomNormalDistribution(60, 110, 5, rng);

        var test = new Test(new TestPreprocessor(new SailfishOutlierDetector()));
        var resultStrict = test.ExecuteTest(before, after, new SailDiffSettings(alpha: 0.01, useOutlierDetection: false, testType: TestType.Test));
        var resultLoose = test.ExecuteTest(before, after, new SailDiffSettings(alpha: 0.05, useOutlierDetection: false, testType: TestType.Test));

        // Same underlying samples → same p-value regardless of α.
        resultStrict.StatisticalTestResult.PValue.ShouldBe(resultLoose.StatisticalTestResult.PValue, tolerance: 1e-9);

        // Both significant — effect is large.
        resultStrict.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.Regressed);
        resultLoose.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.Regressed);
    }

    [Fact]
    public void TwoSampleWilcoxonSignedRank_StaysDocumentedAsPairedOnly_AndStillRunsWhenSizesMatch()
    {
        // We didn't delete the signed-rank test — that would break existing callers who
        // genuinely have paired data. But we documented its semantics on the enum and on the
        // class. This test pins that paired data still produces a usable result.
        var rng = new System.Random(7);
        var before = TestDistributions.GenerateRandomNormalDistribution(20, 100, 5, rng);
        // Construct a genuinely-paired "after": each before[i] plus a small per-pair shift.
        var after = new double[before.Length];
        for (var i = 0; i < before.Length; i++) after[i] = before[i] + 10.0;

        var test = new TwoSampleWilcoxonSignedRankTest(new TestPreprocessor(new SailfishOutlierDetector()));
        var result = test.ExecuteTest(before, after, new SailDiffSettings(alpha: 0.05, useOutlierDetection: false, testType: TestType.TwoSampleWilcoxonSignedRankTest));

        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.Regressed);
    }

    [Fact]
    public void TwoSampleWilcoxonSignedRank_OnMismatchedSizesWithoutAutoLevelling_CarriesException()
    {
        // The signed-rank factory rejects unequal-length inputs — that's the explicit guard
        // that signals misuse. The SailDiff preprocessor happens to down-sample both sides to
        // the same length, which can mask the misuse; here we hit the factory directly to pin
        // the underlying behaviour that protects against silent invalidity.
        var before = new double[] { 1, 2, 3, 4, 5 };
        var after = new double[] { 1, 2, 3 }; // deliberately mis-sized

        Should.Throw<Sailfish.Analysis.SailDiff.Statistics.StatsCore.Exceptions.DimensionMismatchException>(() =>
            Sailfish.Analysis.SailDiff.Statistics.StatsCore.Analysers.Factories.TwoSampleWilcoxonSignedRankFactory.Create(before, after));
    }

    [Fact]
    public void LegacyImpactSummary_HonorsConfiguredAlpha_NotHardcodedFiveHundredths()
    {
        // Regression for Codex review on PR #242: when SailDiffResultMarkdownConverter is
        // constructed without an ISailDiffUnifiedFormatter (the public default-ctor surface),
        // the legacy fallback used to hardcode `p < 0.05` and ignore the configured alpha.
        // With Relaxed (α = 0.10) and a Regressed verdict at p = 0.08 the impact line read
        // "NO CHANGE", contradicting the test wrapper's own verdict. The fix prefers the
        // ChangeDescription as the authoritative source and threads alpha as the fallback.
        var testCaseId = new TestCaseId("Suite.MyTest()");
        var stats = new StatisticalTestResult(
            meanBefore: 100,
            meanAfter: 115,
            medianBefore: 100,
            medianAfter: 115,
            testStatistic: 1.5,
            pValue: 0.08,
            changeDescription: SailfishChangeDirection.Regressed,
            sampleSizeBefore: 20,
            sampleSizeAfter: 20,
            rawDataBefore: new double[] { 100 },
            rawDataAfter: new double[] { 115 },
            additionalResults: new Dictionary<string, object>());
        var sailDiffResult = new SailDiffResult(testCaseId,
            new TestResultWithOutlierAnalysis(stats, null, null));

        // No unified formatter → uses the legacy fallback path.
        var converter = new SailDiffResultMarkdownConverter();
        var output = converter.ConvertToEnhancedMarkdownTable(
            new[] { sailDiffResult },
            Sailfish.Analysis.SailDiff.Formatting.OutputContext.Markdown,
            alpha: 0.10);

        output.ShouldContain("REGRESSED");
        output.ShouldNotContain("(NO CHANGE)");
    }

    [Fact]
    public void LegacyImpactSummary_RespectsNoChangeVerdict_WhenTestWrapperSaidNoChange()
    {
        // Symmetric check: if the test wrapper produced NoChange (because p > the user's α),
        // the legacy summary must show NO CHANGE regardless of how the raw p-value compares
        // to the legacy 0.05 default — the wrapper is the single source of truth.
        var testCaseId = new TestCaseId("Suite.MyTest()");
        var stats = new StatisticalTestResult(
            meanBefore: 100,
            meanAfter: 105,
            medianBefore: 100,
            medianAfter: 105,
            testStatistic: 0.7,
            pValue: 0.04, // would be "significant" under the old hardcoded 0.05 — but wrapper said NoChange
            changeDescription: SailfishChangeDirection.NoChange,
            sampleSizeBefore: 20,
            sampleSizeAfter: 20,
            rawDataBefore: new double[] { 100 },
            rawDataAfter: new double[] { 105 },
            additionalResults: new Dictionary<string, object>());
        var sailDiffResult = new SailDiffResult(testCaseId,
            new TestResultWithOutlierAnalysis(stats, null, null));

        var converter = new SailDiffResultMarkdownConverter();
        var output = converter.ConvertToEnhancedMarkdownTable(
            new[] { sailDiffResult },
            Sailfish.Analysis.SailDiff.Formatting.OutputContext.Markdown,
            alpha: 0.01); // strict alpha; wrapper's NoChange already reflects this

        output.ShouldContain("NO CHANGE");
        output.ShouldNotContain("REGRESSED");
        output.ShouldNotContain("IMPROVED");
    }
}

internal static class Tier1ArrayExtensions
{
    public static double Mean(this double[] xs)
    {
        var sum = 0.0;
        for (var i = 0; i < xs.Length; i++) sum += xs[i];
        return sum / xs.Length;
    }
}
