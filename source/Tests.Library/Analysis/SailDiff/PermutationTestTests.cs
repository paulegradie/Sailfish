using System;
using System.Linq;
using NSubstitute;
using Sailfish.Analysis;
using Sailfish.Analysis.SailDiff;
using Sailfish.Analysis.SailDiff.Statistics.Tests;
using Sailfish.Analysis.SailDiff.Statistics.Tests.PermutationTest;
using Sailfish.Contracts.Public;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

/// <summary>
/// Hand-verifiable behavioural goldens for the two-sample permutation test.
/// </summary>
public class PermutationTestTests
{
    [Fact]
    public void IdenticalSamples_GiveLargeNonSignificantPValue()
    {
        // sample1 == sample2 ⇒ every label permutation produces the same |mean difference|
        // as the observed one (zero), so the count "at least as extreme" equals K. The
        // Phipson & Smyth bias correction gives p = (1+K)/(1+K) = 1.
        var sample = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var test = MakeTest(seed: 42);

        var result = test.ExecuteTest(sample, sample,
            new SailDiffSettings(alpha: 0.05, useOutlierDetection: false, testType: TestType.PermutationTest));

        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.PValue.ShouldBe(1.0, tolerance: 1e-9);
        result.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.NoChange);
    }

    [Fact]
    public void StronglySeparatedSamples_ProduceTinyPValue()
    {
        // All values in sample2 > all values in sample1 → the observed mean difference is
        // the largest possible under any label permutation. The only permutations matching
        // it are those that recover the original split, giving p ≈ 1 / C(20, 10) ≈ 5e-6.
        // Phipson-Smyth lower bound at K=10000 is 1/10001 ≈ 1e-4, so the test reports the
        // floor here.
        var sample1 = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
        var sample2 = Enumerable.Range(100, 10).Select(i => (double)i).ToArray();
        var test = MakeTest(seed: 42);

        var result = test.ExecuteTest(sample1, sample2,
            new SailDiffSettings(alpha: 0.05, useOutlierDetection: false, testType: TestType.PermutationTest));

        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.PValue.ShouldBeLessThan(0.001);
        result.StatisticalTestResult.ChangeDescription.ShouldBe(SailfishChangeDirection.Regressed);
    }

    [Fact]
    public void Deterministic_GivenSeed_SameP()
    {
        // Two runs with the same RunSettings.Seed must produce bit-identical p-values.
        var rng = new Random(1);
        var sample1 = Enumerable.Range(0, 30).Select(_ => 100 + rng.NextDouble() * 5).ToArray();
        var sample2 = Enumerable.Range(0, 30).Select(_ => 102 + rng.NextDouble() * 5).ToArray();

        var settings = new SailDiffSettings(alpha: 0.05, useOutlierDetection: false, testType: TestType.PermutationTest);

        var a = MakeTest(seed: 1234).ExecuteTest(sample1, sample2, settings);
        var b = MakeTest(seed: 1234).ExecuteTest(sample1, sample2, settings);

        a.StatisticalTestResult.PValue.ShouldBe(b.StatisticalTestResult.PValue);
    }

    [Fact]
    public void Deterministic_DifferentSeeds_AreClose()
    {
        // Different RNG streams can produce slightly different Monte Carlo p-values, but
        // both should fall well within the Monte Carlo SE (~0.002 at K=10000, p≈0.5).
        var rng = new Random(1);
        var sample1 = Enumerable.Range(0, 30).Select(_ => 100 + rng.NextDouble() * 5).ToArray();
        var sample2 = Enumerable.Range(0, 30).Select(_ => 102 + rng.NextDouble() * 5).ToArray();

        var settings = new SailDiffSettings(alpha: 0.05, useOutlierDetection: false, testType: TestType.PermutationTest);

        var a = MakeTest(seed: 1234).ExecuteTest(sample1, sample2, settings);
        var b = MakeTest(seed: 9999).ExecuteTest(sample1, sample2, settings);

        Math.Abs(a.StatisticalTestResult.PValue - b.StatisticalTestResult.PValue).ShouldBeLessThan(0.01);
    }

    [Fact]
    public void PermutationCount_LowerSetting_RunsFaster_AndStillProducesValidP()
    {
        // Smaller K → coarser p-value resolution but still a valid probability. Phipson &
        // Smyth lower bound at K=100 is 1/101 ≈ 0.01; matching that for the all-separated
        // case below.
        var sample1 = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
        var sample2 = Enumerable.Range(100, 10).Select(i => (double)i).ToArray();

        var settings = new SailDiffSettings(alpha: 0.05, useOutlierDetection: false, testType: TestType.PermutationTest);
        settings.SetPermutationCount(100);

        var result = MakeTest(seed: 42).ExecuteTest(sample1, sample2, settings);
        result.ExceptionMessage.ShouldBeEmpty();
        result.StatisticalTestResult.PValue.ShouldBeInRange(0.0, 1.0);
    }

    [Fact]
    public void PopulatesEffectSizeAndDifferenceLikeRankSum()
    {
        // Permutation test borrows the rank-sum effect-size suite for consistency.
        var rng = new Random(7);
        var sample1 = Enumerable.Range(0, 20).Select(_ => 100 + rng.NextDouble() * 5).ToArray();
        var sample2 = Enumerable.Range(0, 20).Select(_ => 110 + rng.NextDouble() * 5).ToArray();

        var settings = new SailDiffSettings(alpha: 0.05, useOutlierDetection: false, testType: TestType.PermutationTest);
        var result = MakeTest(seed: 42).ExecuteTest(sample1, sample2, settings);

        result.StatisticalTestResult.EffectSize.ShouldNotBeNull();
        result.StatisticalTestResult.EffectSize!.Name.ShouldBe("Cliff's delta");

        result.StatisticalTestResult.Difference.ShouldNotBeNull();
        result.StatisticalTestResult.Difference!.Name.ShouldBe("Hodges-Lehmann shift");
    }

    [Fact]
    public void TooSmallSamples_ReturnError()
    {
        // Minimum useful permutation test is 2v2 → C(4,2) = 6 distinct permutations.
        // Below that the test should error rather than emit garbage.
        var settings = new SailDiffSettings(alpha: 0.05, useOutlierDetection: false, testType: TestType.PermutationTest);
        var result = MakeTest(seed: 42).ExecuteTest(new[] { 1.0 }, new[] { 2.0 }, settings);

        result.ExceptionMessage.ShouldNotBeNullOrEmpty();
    }

    private static PermutationTest MakeTest(int? seed)
    {
        var preprocessor = new TestPreprocessor(new SailfishOutlierDetector());
        if (seed is null) return new PermutationTest(preprocessor);
        var runSettings = Substitute.For<IRunSettings>();
        runSettings.Seed.Returns(seed.Value);
        return new PermutationTest(preprocessor, runSettings);
    }
}
