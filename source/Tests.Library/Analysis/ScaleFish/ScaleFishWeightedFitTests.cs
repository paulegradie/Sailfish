using System;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Sailfish.Analysis.ScaleFish.CurveFitting;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Verifies that the weighted least-squares path is activated when uncertainty information is available
/// and that it produces sensible parameters. Unequal weights should pull the fit toward the
/// lower-uncertainty points.
/// </summary>
public class ScaleFishWeightedFitTests
{
    [Fact]
    public void BuildVarianceWeights_NoUncertainty_ReturnsNull()
    {
        var measurements = new[]
        {
            new ComplexityMeasurement(1, 1.0),
            new ComplexityMeasurement(2, 2.0)
        };
        ScaleFishModelFunction.BuildVarianceWeights(measurements).ShouldBeNull();
    }

    [Fact]
    public void BuildVarianceWeights_AllUncertainty_ProducesPositiveWeights()
    {
        var measurements = new[]
        {
            new ComplexityMeasurement(1, 1.0, stdDev: 0.1, sampleSize: 10),
            new ComplexityMeasurement(2, 2.0, stdDev: 0.4, sampleSize: 10),
            new ComplexityMeasurement(3, 3.0, stdDev: 0.1, sampleSize: 10)
        };
        var weights = ScaleFishModelFunction.BuildVarianceWeights(measurements);
        weights.ShouldNotBeNull();
        weights.Length.ShouldBe(3);
        weights.ShouldAllBe(w => w > 0);
        // Larger SE ⇒ smaller weight
        weights[1].ShouldBeLessThan(weights[0]);
        weights[1].ShouldBeLessThan(weights[2]);
    }

    [Fact]
    public void BuildVarianceWeights_PartialUncertainty_ReturnsNull()
    {
        // If any measurement is missing uncertainty, fall back to unweighted fit (deterministic behaviour).
        var measurements = new[]
        {
            new ComplexityMeasurement(1, 1.0, stdDev: 0.1, sampleSize: 10),
            new ComplexityMeasurement(2, 2.0)
        };
        ScaleFishModelFunction.BuildVarianceWeights(measurements).ShouldBeNull();
    }

    [Fact]
    public void FitLinearInParameters_UnweightedMatchesOlsClosedForm()
    {
        // y = 3x + 2, evaluated at x = {1, 2, 3, 4}
        var measurements = new[]
        {
            new ComplexityMeasurement(1, 5),
            new ComplexityMeasurement(2, 8),
            new ComplexityMeasurement(3, 11),
            new ComplexityMeasurement(4, 14)
        };
        var calc = new FitnessCalculator();
        var fit = calc.FitLinearInParameters(measurements, x => x);

        fit.Scale.ShouldBe(3.0, tolerance: 1e-9);
        fit.Bias.ShouldBe(2.0, tolerance: 1e-9);
    }

    [Fact]
    public void FitLinearInParameters_WeightedPullsTowardHighWeightPoints()
    {
        // True linear y = 2x. The middle point is a deliberate outlier with low weight.
        var measurements = new[]
        {
            new ComplexityMeasurement(1, 2),
            new ComplexityMeasurement(2, 20),    // outlier
            new ComplexityMeasurement(3, 6),
            new ComplexityMeasurement(4, 8)
        };
        var weights = new[] { 1.0, 0.01, 1.0, 1.0 };

        var calc = new FitnessCalculator();
        var weighted = calc.FitLinearInParameters(measurements, x => x, weights);
        var unweighted = calc.FitLinearInParameters(measurements, x => x);

        // Weighted scale should be close to 2 (true), unweighted is dragged up by the outlier.
        Math.Abs(weighted.Scale - 2.0).ShouldBeLessThan(Math.Abs(unweighted.Scale - 2.0));
    }

    [Fact]
    public void FitLinearInParameters_RejectsMismatchedWeights()
    {
        var measurements = new[]
        {
            new ComplexityMeasurement(1, 1),
            new ComplexityMeasurement(2, 2)
        };
        var calc = new FitnessCalculator();
        Should.Throw<Sailfish.Exceptions.SailfishException>(
            () => calc.FitLinearInParameters(measurements, x => x, new[] { 1.0, 2.0, 3.0 }));
    }

    [Fact]
    public void FitLinearInParameters_TooFewPoints_Throws()
    {
        var measurements = new[] { new ComplexityMeasurement(1, 1) };
        var calc = new FitnessCalculator();
        Should.Throw<Sailfish.Exceptions.SailfishException>(
            () => calc.FitLinearInParameters(measurements, x => x));
    }

    [Fact]
    public void NoisyLinear_WeightedFitConverges()
    {
        // Heteroskedastic noise: small SE at small X, large SE at large X. The weighted fit should
        // still converge to Linear as the best family.
        var rng = new Random(31);
        var xs = ScaleFishTestHelpers.LogSpacedX(8, 512, 6);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => 2.0 * x,
            xs,
            sampleSize: 30,
            relativeNoise: 0.04,
            rng);

        var result = new ComplexityEstimator().EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.ScaleFishModelFunction.Name.ShouldBe(nameof(Linear));
        result.ScaleFishModelFunction.FunctionParameters!.Scale.ShouldBe(2.0, tolerance: 0.15);
    }
}
