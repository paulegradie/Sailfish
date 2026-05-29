using System;
using System.Linq;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Verifies the <see cref="AdaptiveXRecommender"/> planning utility: geometric initial probe, refinement
/// against a prior model, and the input-validation contracts.
/// </summary>
public class ScaleFishAdaptiveXTests
{
    [Fact]
    public void InitialProbe_IsGeometric()
    {
        var xs = AdaptiveXRecommender.RecommendInitialProbe(8, 256, points: 6);
        xs.ShouldBe(new[] { 8, 16, 32, 64, 128, 256 });
    }

    [Fact]
    public void InitialProbe_DefaultPointCount()
    {
        var xs = AdaptiveXRecommender.RecommendInitialProbe(4, 128);
        xs.Count.ShouldBe(6);
        xs[0].ShouldBe(4);
        xs[^1].ShouldBe(128);
    }

    [Fact]
    public void InitialProbe_RejectsImpossibleRequests()
    {
        Should.Throw<ArgumentException>(() => AdaptiveXRecommender.RecommendInitialProbe(1, 2, points: 5));
        Should.Throw<ArgumentException>(() => AdaptiveXRecommender.RecommendInitialProbe(0, 100));
        Should.Throw<ArgumentException>(() => AdaptiveXRecommender.RecommendInitialProbe(100, 50));
    }

    [Fact]
    public void Refinement_ExtendsPastCurrentMax()
    {
        var measurements = ScaleFishTestHelpers.BuildExact(new Linear(),
            ScaleFishTestHelpers.LogSpacedX(8, 256, 6));
        var prior = new ComplexityEstimator().EstimateComplexity(measurements);
        prior.ShouldNotBeNull();

        var existing = new[] { 8, 16, 32, 64, 128, 256 };
        var refined = AdaptiveXRecommender.RecommendRefinement(existing, prior, targetMaxN: 2048, extraPoints: 3);

        // Existing values preserved, sorted, with extras added past the current max.
        refined.Take(6).ShouldBe(existing);
        refined[^1].ShouldBeLessThanOrEqualTo(2048);
        refined[^1].ShouldBeGreaterThan(256);
        refined.Count.ShouldBeGreaterThan(existing.Length);
    }

    [Fact]
    public void Refinement_TargetBelowMax_ReturnsExisting()
    {
        var measurements = ScaleFishTestHelpers.BuildExact(new Linear(),
            ScaleFishTestHelpers.LogSpacedX(8, 256, 6));
        var prior = new ComplexityEstimator().EstimateComplexity(measurements);
        prior.ShouldNotBeNull();

        var existing = new[] { 8, 16, 32, 64, 128, 256 };
        var refined = AdaptiveXRecommender.RecommendRefinement(existing, prior, targetMaxN: 100);
        // 100 < 256 → no extension; return the existing set sorted/deduped.
        refined.ShouldBe(existing);
    }

    [Fact]
    public void Refinement_RejectsEmptyPrior()
    {
        var dummy = new ComplexityEstimator().EstimateComplexity(
            ScaleFishTestHelpers.BuildExact(new Linear(), new[] { 1, 2, 3, 4 }));
        dummy.ShouldNotBeNull();
        Should.Throw<ArgumentException>(
            () => AdaptiveXRecommender.RecommendRefinement(Array.Empty<int>(), dummy, 100));
    }

    [Fact]
    public void Refinement_NarrowExtensionRange_DegradesInsteadOfThrowing()
    {
        // Regression: when targetMaxN is only one or two integers past currentMax, the underlying
        // SpacedRange would refuse to emit `extraPoints` distinct integers. The recommender should
        // cap extraPoints to what's feasible rather than throwing.
        var measurements = ScaleFishTestHelpers.BuildExact(new Linear(), new[] { 4, 8, 16, 32 });
        var prior = new ComplexityEstimator().EstimateComplexity(measurements);
        prior.ShouldNotBeNull();

        var existing = new[] { 4, 8, 16, 32 };

        // Only one available integer past currentMax (33), but the caller asks for 3 extras —
        // recommender should append the single available point rather than throw.
        var refined = AdaptiveXRecommender.RecommendRefinement(existing, prior, targetMaxN: 33, extraPoints: 3);
        refined[^1].ShouldBeLessThanOrEqualTo(33);
        refined[^1].ShouldBeGreaterThan(32);
    }

    [Fact]
    public void Refinement_TwoIntegerExtension_ProducesPair()
    {
        var measurements = ScaleFishTestHelpers.BuildExact(new Linear(), new[] { 4, 8, 16, 32 });
        var prior = new ComplexityEstimator().EstimateComplexity(measurements);
        prior.ShouldNotBeNull();

        var existing = new[] { 4, 8, 16, 32 };
        // Range of 2 available integers (33, 34) — should cap extraPoints from 3 to 2.
        var refined = AdaptiveXRecommender.RecommendRefinement(existing, prior, targetMaxN: 34, extraPoints: 3);
        refined.Count(x => x > 32).ShouldBeGreaterThanOrEqualTo(1);
        refined[^1].ShouldBeLessThanOrEqualTo(34);
    }
}
