using System;
using System.Linq;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Sailfish.Analysis.ScaleFish.CurveFitting;
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

    [Fact]
    public void NLogNData_WithReplicates_IsDistinguishable()
    {
        // Regression for the LogLinear clone: with both x·ln(x) and x·log₂(x) in the candidate set,
        // n·log n data always produced ΔAICc ≈ 0 between the two identical fits, so this could never
        // be distinguishable no matter how clean the data.
        var nlogn = new NLogN();
        var rng = new Random(7);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => nlogn.Compute(0.0, 1.0, x),
            ScaleFishTestHelpers.LogSpacedX(8, 1024, 6),
            sampleSize: 30,
            relativeNoise: 0.05,
            rng);

        var result = new ComplexityEstimator().EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.ScaleFishModelFunction.Name.ShouldBe(nameof(NLogN));
        result.IsDistinguishable.ShouldBeTrue("n·log n no longer ties with its own collinear clone");
    }

    [Fact]
    public void ThreeXValues_WithReplicates_CanDistinguish()
    {
        // Regression for means-level AICc degeneracy: with n = 3 X values and k = 2 parameters the
        // small-sample correction divides by n − k − 1 = 0, so every family scored +∞, the Akaike
        // weight was NaN, and no 3-value declaration could ever be distinguishable. Replicate-level
        // scoring uses N = Σ replicates, so the information in the per-X spread finally counts.
        var quadratic = new Quadratic();
        var rng = new Random(11);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => quadratic.Compute(0.0, 1.0, x),
            new[] { 10, 100, 1000 },
            sampleSize: 15,
            relativeNoise: 0.05,
            rng);

        var result = new ComplexityEstimator().EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.ScaleFishModelFunction.Name.ShouldBe(nameof(Quadratic));
        result.IsDistinguishable.ShouldBeTrue();
        double.IsFinite(result.BestAicc).ShouldBeTrue();
        double.IsFinite(result.AkaikeWeight).ShouldBeTrue();
        result.AkaikeWeight.ShouldBeGreaterThan(0.5);
    }

    [Fact]
    public void ThreeXValues_MeansOnly_RemainsIndistinguishable()
    {
        // Without replicate uncertainty there is no honest way to select among two-parameter
        // families from three means; the means-level fallback keeps reporting that.
        var quadratic = new Quadratic();
        var measurements = ScaleFishTestHelpers.BuildExact(quadratic, new[] { 10, 100, 1000 });

        var result = new ComplexityEstimator().EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.IsDistinguishable.ShouldBeFalse();
    }

    [Fact]
    public void ReplicateAicc_LowerForBetterFittingParameters()
    {
        var measurements = new[]
        {
            new ComplexityMeasurement(10, 20, stdDev: 1.0, sampleSize: 10),
            new ComplexityMeasurement(20, 40, stdDev: 1.0, sampleSize: 10),
            new ComplexityMeasurement(40, 80, stdDev: 1.0, sampleSize: 10)
        };
        var inverseSquaredSes = new[] { 10.0, 10.0, 10.0 }; // SE = 1/√10 ⇒ 1/SE² = 10

        var exact = new Linear { FunctionParameters = new FittedCurve(scale: 2.0, bias: 0.0) };
        var off = new Linear { FunctionParameters = new FittedCurve(scale: 2.5, bias: 0.0) };

        var exactAicc = ComplexityEstimator.ComputeReplicateAicc(exact, measurements, inverseSquaredSes, totalReplicates: 30, k: 2);
        var offAicc = ComplexityEstimator.ComputeReplicateAicc(off, measurements, inverseSquaredSes, totalReplicates: 30, k: 2);

        exactAicc.ShouldBeLessThan(offAicc);
        // Perfect fit ⇒ χ² = 0 ⇒ AICc reduces to the parameter penalty: 2k + 2k(k+1)/(N−k−1).
        exactAicc.ShouldBe(4.0 + 12.0 / 27.0, tolerance: 1e-9);
    }

    [Fact]
    public void FlatReplicateData_ConstantWinsViaParameterPenalty()
    {
        // Identical means at every X with genuine replicate uncertainty: every family fits the data
        // exactly (scale = 0), so χ² = 0 across the board and the ranking is decided purely by the
        // AICc parameter penalty — the first time k matters. Constant (k = 1) must beat every
        // two-parameter family by ΔAICc ≈ 2, i.e. distinguishably.
        var measurements = new[] { 8, 16, 32, 64, 128, 256 }
            .Select(x => new ComplexityMeasurement(x, 100.0, stdDev: 5.0, sampleSize: 20))
            .ToArray();

        var result = new ComplexityEstimator().EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.ScaleFishModelFunction.Name.ShouldBe("Constant");
        result.IsDistinguishable.ShouldBeTrue("k = 1 vs k = 2 yields ΔAICc ≥ 2 when residuals tie");
        result.ScaleFishModelFunction.FunctionParameters!.Bias.ShouldBe(100.0, tolerance: 1e-9);
    }

    [Fact]
    public void NoisyFlatData_SurfacesAsConstantOrFlatFit()
    {
        // With real noise the AIC parsimony edge is probabilistic per seed (a spurious slope can
        // buy a two-parameter family up to ~χ²₁ of fit), so the user-facing contract is the
        // combination: either the Constant family wins outright, or the winner is an effectively
        // flat curve that ConstantComplexityDetector flags. Either way the user is told the
        // variable isn't driving the runtime.
        for (var seed = 1; seed <= 10; seed++)
        {
            var rng = new Random(seed);
            var measurements = ScaleFishTestHelpers.BuildNoisy(
                _ => 250.0,
                new[] { 8, 16, 32, 64, 128, 256 },
                sampleSize: 25,
                relativeNoise: 0.05,
                rng);

            var result = new ComplexityEstimator().EstimateComplexity(measurements);

            result.ShouldNotBeNull($"seed {seed} produced no result");
            var flagged = result.ScaleFishModelFunction.Name == "Constant"
                          || ConstantComplexityDetector.IsLikelyConstant(result, measurements);
            flagged.ShouldBeTrue(
                $"seed {seed}: winner {result.ScaleFishModelFunction.Name} was neither Constant nor flagged flat");
        }
    }

    [Fact]
    public void ReplicateAicc_DegenerateInputs_ReturnInfinity()
    {
        var measurements = new[]
        {
            new ComplexityMeasurement(10, 20, stdDev: 1.0, sampleSize: 1),
            new ComplexityMeasurement(20, 40, stdDev: 1.0, sampleSize: 1)
        };
        var inverseSquaredSes = new[] { 1.0, 1.0 };

        // Unfitted function abstains.
        var unfitted = new Linear();
        ComplexityEstimator.ComputeReplicateAicc(unfitted, measurements, inverseSquaredSes, totalReplicates: 2, k: 2)
            .ShouldBe(double.PositiveInfinity);

        // Total replicates ≤ k + 1 leaves the small-sample correction undefined.
        var fitted = new Linear { FunctionParameters = new FittedCurve(scale: 2.0, bias: 0.0) };
        ComplexityEstimator.ComputeReplicateAicc(fitted, measurements, inverseSquaredSes, totalReplicates: 3, k: 2)
            .ShouldBe(double.PositiveInfinity);
    }
}
