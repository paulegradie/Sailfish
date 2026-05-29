using System;
using System.Linq;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Per-X percentile fits: when raw replicates are present, ScaleFish should classify p50/p95/p99 separately
/// so users can see whether the tail scales differently from the mean.
/// </summary>
public class ScaleFishTailFitTests
{
    [Fact]
    public void DefaultSettings_ProduceP50P95P99()
    {
        var rng = new Random(101);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => x,
            ScaleFishTestHelpers.LogSpacedX(8, 256, 6),
            sampleSize: 50,
            relativeNoise: 0.05,
            rng);

        var result = new ComplexityEstimator().EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.TailFits.Count.ShouldBe(3);
        var pcts = result.TailFits.Select(t => t.Percentile).OrderBy(p => p).ToArray();
        pcts[0].ShouldBe(0.50, tolerance: 1e-9);
        pcts[1].ShouldBe(0.95, tolerance: 1e-9);
        pcts[2].ShouldBe(0.99, tolerance: 1e-9);
    }

    [Fact]
    public void Disabled_OmitsTailFits()
    {
        var rng = new Random(102);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => x,
            ScaleFishTestHelpers.LogSpacedX(8, 256, 6),
            sampleSize: 30,
            relativeNoise: 0.05,
            rng);

        var settings = new ScaleFishSettings { EnableTailPercentileFits = false };
        var result = new ComplexityEstimator(settings).EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.TailFits.Count.ShouldBe(0);
    }

    [Fact]
    public void NoRawSamples_OmitsTailFits()
    {
        // Exact synthetic measurements have no RawSamples — tail fits should skip.
        var measurements = ScaleFishTestHelpers.BuildExact(new Linear(),
            ScaleFishTestHelpers.LogSpacedX(8, 256, 6));
        var result = new ComplexityEstimator().EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.TailFits.Count.ShouldBe(0);
    }

    [Fact]
    public void CustomPercentiles_Honored()
    {
        var rng = new Random(103);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => x * x,
            ScaleFishTestHelpers.LogSpacedX(4, 256, 6),
            sampleSize: 30,
            relativeNoise: 0.05,
            rng);

        var settings = new ScaleFishSettings { TailPercentiles = new[] { 0.25, 0.75 } };
        var result = new ComplexityEstimator(settings).EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.TailFits.Count.ShouldBe(2);
        result.TailFits.Select(t => t.Percentile).ShouldBe(new[] { 0.25, 0.75 });
    }

    [Fact]
    public void TailFit_OfLinearData_ClassifiesAsLinear()
    {
        var rng = new Random(104);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => 3.0 * x,
            ScaleFishTestHelpers.LogSpacedX(8, 1024, 6),
            sampleSize: 50,
            relativeNoise: 0.03,
            rng);

        var result = new ComplexityEstimator().EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        foreach (var fit in result.TailFits)
        {
            fit.BestFamilyName.ShouldBe(nameof(Linear), $"p{fit.Percentile:F2} should classify as Linear");
        }
    }

    [Fact]
    public void OutOfRangePercentiles_Skipped()
    {
        var rng = new Random(105);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => x,
            ScaleFishTestHelpers.LogSpacedX(8, 256, 6),
            sampleSize: 30,
            relativeNoise: 0.05,
            rng);

        var settings = new ScaleFishSettings { TailPercentiles = new[] { -0.1, 0.5, 1.1, 0.95 } };
        var result = new ComplexityEstimator(settings).EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.TailFits.Count.ShouldBe(2);
        result.TailFits.Select(t => t.Percentile).ShouldBe(new[] { 0.5, 0.95 });
    }
}
