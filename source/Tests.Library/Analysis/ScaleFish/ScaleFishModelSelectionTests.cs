using System;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Verifies the AICc-based model selection: information-criterion values, Akaike weight,
/// and the IsDistinguishable flag that gates "confident classification" calls.
/// </summary>
public class ScaleFishModelSelectionTests
{
    [Fact]
    public void Aicc_DropsAsResidualShrinks()
    {
        // Lower RSS ⇒ lower AICc, holding n and k constant.
        var aiccHighRss = ComplexityEstimator.ComputeAicc(rss: 100.0, n: 6, k: 2);
        var aiccLowRss = ComplexityEstimator.ComputeAicc(rss: 1.0, n: 6, k: 2);
        aiccLowRss.ShouldBeLessThan(aiccHighRss);
    }

    [Fact]
    public void Aicc_PenalisesMoreParameters()
    {
        // For the same RSS and n, more parameters ⇒ worse (higher) AICc.
        var aicc2 = ComplexityEstimator.ComputeAicc(rss: 10.0, n: 6, k: 2);
        var aicc3 = ComplexityEstimator.ComputeAicc(rss: 10.0, n: 6, k: 3);
        aicc3.ShouldBeGreaterThan(aicc2);
    }

    [Fact]
    public void Aicc_DegenerateInputs_ReturnInfinity()
    {
        ComplexityEstimator.ComputeAicc(rss: -1, n: 6, k: 2).ShouldBe(double.PositiveInfinity);
        ComplexityEstimator.ComputeAicc(rss: double.NaN, n: 6, k: 2).ShouldBe(double.PositiveInfinity);
        // Small-sample correction undefined when n - k - 1 ≤ 0
        ComplexityEstimator.ComputeAicc(rss: 1.0, n: 3, k: 2).ShouldBe(double.PositiveInfinity);
    }

    [Fact]
    public void AkaikeWeight_DegeneratesToSingleWinner()
    {
        // One model decisively better → its weight ≈ 1.
        var weight = ComplexityEstimator.ComputeAkaikeWeightOfBest(new[] { 0.0, 100.0, 200.0 });
        weight.ShouldBeGreaterThan(0.99);
    }

    [Fact]
    public void AkaikeWeight_EquallySupportedModels_SplitsEvenly()
    {
        var weight = ComplexityEstimator.ComputeAkaikeWeightOfBest(new[] { 0.0, 0.0, 0.0, 0.0 });
        weight.ShouldBe(0.25, tolerance: 1e-9);
    }

    [Fact]
    public void ExactLinearData_DistinguishableFromAllOthers()
    {
        var linear = new Linear();
        var measurements = ScaleFishTestHelpers.BuildExact(linear, new[] { 4, 8, 16, 32, 64, 128 });
        var result = new ComplexityEstimator().EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.ScaleFishModelFunction.Name.ShouldBe(nameof(Linear));
        result.IsDistinguishable.ShouldBeTrue("noise-free exact Linear data should be unambiguously classifiable");
        result.DeltaAicc.ShouldBeGreaterThan(2.0);
        result.AkaikeWeight.ShouldBeGreaterThan(0.99);
        result.SampleSize.ShouldBe(measurements.Length);
    }

    [Fact]
    public void Linear_vs_Cubic_NoisyButClearlySeparable()
    {
        // Cubic with even small noise is very different from Linear over a log-spaced range.
        var cubic = new Cubic();
        var rng = new Random(42);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => cubic.Compute(0, 1, x),
            ScaleFishTestHelpers.LogSpacedX(4, 128, 6),
            sampleSize: 30,
            relativeNoise: 0.05,
            rng);

        var result = new ComplexityEstimator().EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.ScaleFishModelFunction.Name.ShouldBe(nameof(Cubic));
        result.IsDistinguishable.ShouldBeTrue();
    }

    [Fact]
    public void TooFewMeasurements_ReturnsNull()
    {
        new ComplexityEstimator()
            .EstimateComplexity(new[] { new ComplexityMeasurement(1, 1) })
            .ShouldBeNull();

        new ComplexityEstimator()
            .EstimateComplexity(Array.Empty<ComplexityMeasurement>())
            .ShouldBeNull();
    }
}
