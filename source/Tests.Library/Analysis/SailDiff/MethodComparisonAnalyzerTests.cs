using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Analysis.SailDiff;
using Sailfish.Contracts.Public.Models;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.SailDiff;

/// <summary>
/// Locks the unified method-comparison verdict: significance comes from the configured SailDiff test on the
/// raw samples (with one BH-FDR pass per cohort), the ratio + CI is a separate effect size, and a
/// maximally-separated comparison (exact p == 0) still reads as significant rather than "Similar".
/// </summary>
public class MethodComparisonAnalyzerTests
{
    private static readonly IMethodComparisonAnalyzer Analyzer = Tests.Common.MethodComparisonAnalyzerTestFactory.Create();
    private static readonly SailDiffSettings Settings = new(alpha: 0.05);

    // Deterministic samples: n values linearly spanning center ± spread (so there is real within-group
    // variance for the ratio CI, and no reliance on randomness).
    private static double[] Samples(double center, double spread, int n)
    {
        if (n == 1) return new[] { center };
        return Enumerable.Range(0, n).Select(i => center - spread + 2 * spread * i / (n - 1)).ToArray();
    }

    private static MethodComparisonMember Member(string name, bool isBaseline, double[] samples)
    {
        var mean = samples.Average();
        var variance = samples.Length > 1 ? samples.Select(x => (x - mean) * (x - mean)).Sum() / (samples.Length - 1) : 0.0;
        var stdDev = Math.Sqrt(variance);
        var ordered = samples.OrderBy(x => x).ToArray();
        var mid = ordered.Length / 2;
        var median = ordered.Length % 2 == 0 ? (ordered[mid - 1] + ordered[mid]) / 2.0 : ordered[mid];
        var result = new PerformanceRunResult(
            name, mean, stdDev, variance, median,
            samples, samples.Length, 0,
            samples, Array.Empty<double>(), Array.Empty<double>(), 0);
        return new MethodComparisonMember(name, name, isBaseline, result);
    }

    [Fact]
    public void NxN_ProducesOnePairPerUnorderedCombination()
    {
        var members = new[]
        {
            Member("A", false, Samples(10, 1, 20)),
            Member("B", false, Samples(20, 1, 20)),
            Member("C", false, Samples(30, 1, 20))
        };

        var result = Analyzer.Analyze(members, Settings);

        result.BaselineMode.ShouldBeFalse();
        result.Pairs.Count.ShouldBe(3); // C(3,2)
    }

    [Fact]
    public void BaselineMode_EveryPairIsBaselineVsContender()
    {
        var members = new[]
        {
            Member("Baseline", true, Samples(10, 1, 20)),
            Member("C1", false, Samples(20, 1, 20)),
            Member("C2", false, Samples(30, 1, 20))
        };

        var result = Analyzer.Analyze(members, Settings);

        result.BaselineMode.ShouldBeTrue();
        result.BaselineId.ShouldBe("Baseline");
        result.Pairs.Count.ShouldBe(2); // N-1
        result.Pairs.ShouldAllBe(p => p.Primary.Id == "Baseline");
    }

    [Fact]
    public void ClearlySlowerContender_IsLabelledSlower_WithSignificantQ()
    {
        // Baseline is fast (~10), contender is far slower (~100) and fully separated.
        var members = new[]
        {
            Member("Baseline", true, Samples(10, 0.5, 15)),
            Member("Slow", false, Samples(100, 5, 15))
        };

        var pair = Analyzer.Analyze(members, Settings).Pairs.Single();

        pair.Verdict.ShouldBe(MethodComparisonVerdict.Slower); // ratio = compared/baseline > 1
        pair.QValue.ShouldBeGreaterThan(0);
        pair.QValue.ShouldBeLessThanOrEqualTo(Settings.Alpha);
        pair.Ratio.ShouldBeGreaterThan(1.0);
    }

    [Fact]
    public void ClearlyFasterContender_IsLabelledImproved()
    {
        var members = new[]
        {
            Member("Baseline", true, Samples(100, 5, 15)),
            Member("Fast", false, Samples(10, 0.5, 15))
        };

        var pair = Analyzer.Analyze(members, Settings).Pairs.Single();

        pair.Verdict.ShouldBe(MethodComparisonVerdict.Improved); // contender faster than baseline
        pair.Ratio.ShouldBeLessThan(1.0);
    }

    [Fact]
    public void HeavilyOverlappingSamples_AreLabelledSimilar()
    {
        // Tiny shift relative to spread → the rank-sum cannot reject; honest "Similar".
        var members = new[]
        {
            Member("A", true, Samples(10.0, 3, 12)),
            Member("B", false, Samples(10.3, 3, 12))
        };

        var pair = Analyzer.Analyze(members, Settings).Pairs.Single();

        pair.Verdict.ShouldBe(MethodComparisonVerdict.Similar);
    }

    [Fact]
    public void MaximalSeparationAtLargeN_StaysSignificant_NotMislabelledSimilar()
    {
        // N > 30 uses the large-sample normal approximation, whose p underflows to exactly 0 for a huge,
        // perfectly-separated gap. Without the analyzer's zero-floor this would read as "Similar" (the
        // canonical "this method is 100× faster" mislabelled). Lock it as significant.
        var members = new[]
        {
            Member("Baseline", true, Samples(1000, 5, 40)),
            Member("Fast", false, Samples(1, 0.05, 40))
        };

        var pair = Analyzer.Analyze(members, Settings).Pairs.Single();

        pair.Verdict.ShouldBe(MethodComparisonVerdict.Improved);
        pair.QValue.ShouldBeGreaterThan(0);
        pair.QValue.ShouldBeLessThanOrEqualTo(Settings.Alpha);
    }

    [Fact]
    public void InsufficientSamples_FallThroughToSimilar()
    {
        // Fewer than 3 samples per side → the test can't run → no significance → Similar (never a crash).
        var members = new[]
        {
            Member("A", true, new[] { 10.0, 10.0 }),
            Member("B", false, new[] { 20.0, 20.0 })
        };

        var pair = Analyzer.Analyze(members, Settings).Pairs.Single();

        pair.Verdict.ShouldBe(MethodComparisonVerdict.Similar);
        double.IsNaN(pair.QValue).ShouldBeTrue();
    }

    [Fact]
    public void Find_LocatesPairRegardlessOfArgumentOrder()
    {
        var members = new[]
        {
            Member("A", false, Samples(10, 1, 20)),
            Member("B", false, Samples(20, 1, 20))
        };

        var result = Analyzer.Analyze(members, Settings);

        result.Find("A", "B").ShouldNotBeNull();
        result.Find("B", "A").ShouldNotBeNull();
        result.Find("A", "B").ShouldBeSameAs(result.Find("B", "A"));
    }

    [Fact]
    public void FewerThanTwoUsableMethods_ProducesNoPairs()
    {
        var members = new[] { Member("Solo", false, Samples(10, 1, 20)) };

        Analyzer.Analyze(members, Settings).Pairs.ShouldBeEmpty();
    }
}
