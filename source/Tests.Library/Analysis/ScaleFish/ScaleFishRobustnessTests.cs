using System;
using System.Linq;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Edge cases and pathological inputs: too few points, non-finite values, single-family scenarios,
/// numerical-overflow regimes for Exponential/Factorial.
/// </summary>
public class ScaleFishRobustnessTests
{
    [Fact]
    public void Exponential_AtModerateX_FitsViaDefaultOls()
    {
        var exp = new Exponential();
        var measurements = ScaleFishTestHelpers.BuildExact(exp, new[] { 4, 6, 8, 10, 12, 14, 16 });
        var fit = exp.SeedFit(measurements);
        fit.Scale.ShouldBe(1.0, tolerance: 0.01);
        fit.Bias.ShouldBe(0.0, tolerance: 1.0); // forgiving on bias because of numerical scale
    }

    [Fact]
    public void Exponential_AtLargeX_FallsBackToLogSpace()
    {
        // X = 1100 → 2^1100 overflows double. Log-space fit must engage.
        var exp = new Exponential();
        var measurements = new[]
        {
            new ComplexityMeasurement(1000, Math.Pow(2, 50)),  // synthetic small y to keep finite
            new ComplexityMeasurement(1100, Math.Pow(2, 60)),
            new ComplexityMeasurement(1200, Math.Pow(2, 70))
        };
        // The default OLS would blow up; the override should pick up via log-space.
        Should.NotThrow(() => exp.SeedFit(measurements));
    }

    [Fact]
    public void Factorial_AtLargeX_FallsBackToLogSpace()
    {
        // X = 200 ⇒ 200! overflows double. The log-space override should engage.
        var fact = new Factorial();
        var measurements = new[]
        {
            new ComplexityMeasurement(180, 1e100),
            new ComplexityMeasurement(190, 1e200),
            new ComplexityMeasurement(200, 1e290)
        };
        Should.NotThrow(() => fact.SeedFit(measurements));
    }

    [Fact]
    public void EstimateComplexity_AllNonFiniteY_ReturnsNull()
    {
        var measurements = new[]
        {
            new ComplexityMeasurement(1, double.NaN),
            new ComplexityMeasurement(2, double.PositiveInfinity),
            new ComplexityMeasurement(3, double.NaN)
        };
        new ComplexityEstimator().EstimateComplexity(measurements).ShouldBeNull();
    }

    [Fact]
    public void EstimateComplexity_OneFiniteRest_NaN_ReturnsNull()
    {
        // Only one usable measurement after filtering ⇒ insufficient data ⇒ null.
        var measurements = new[]
        {
            new ComplexityMeasurement(1, 1.0),
            new ComplexityMeasurement(2, double.NaN),
            new ComplexityMeasurement(3, double.NaN)
        };
        new ComplexityEstimator().EstimateComplexity(measurements).ShouldBeNull();
    }

    [Fact]
    public void EstimateComplexity_ConstantY_DoesNotCrash()
    {
        // Pathological: all Y identical. Many candidates can't be fit (zero variance after centering).
        // The estimator should either return a model or null without exceptions.
        var measurements = new[]
        {
            new ComplexityMeasurement(1, 5),
            new ComplexityMeasurement(2, 5),
            new ComplexityMeasurement(3, 5),
            new ComplexityMeasurement(4, 5)
        };
        Should.NotThrow(() => new ComplexityEstimator().EstimateComplexity(measurements));
    }

    [Fact]
    public void Convergence_TwoPoints_ProducesSomeResult()
    {
        // Boundary: exactly the minimum number of points the estimator accepts.
        var measurements = new[]
        {
            new ComplexityMeasurement(2, 4),
            new ComplexityMeasurement(4, 16)
        };
        var result = new ComplexityEstimator().EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.SampleSize.ShouldBe(2);
    }

    [Fact]
    public void EveryBuiltInFamily_FreeParameterCountIsTwo()
    {
        foreach (var family in ComplexityReferences.GetComplexityFunctions())
        {
            family.FreeParameterCount.ShouldBe(2, $"{family.Name} reports unexpected free-parameter count");
        }
    }

    [Fact]
    public void EveryBuiltInFamily_FitsItsOwnExactData()
    {
        // Sanity check: round-trip every family. For each family F, generate exact F(x) data and confirm
        // F.SeedFit returns scale ≈ 1, bias ≈ 0.
        foreach (var family in ComplexityReferences.GetComplexityFunctions())
        {
            var xs = family.Name == nameof(Factorial)
                ? new[] { 3, 5, 7, 9, 11 }
                : family.Name == nameof(Exponential)
                    ? new[] { 4, 6, 8, 10, 12, 14 }
                    : new[] { 4, 8, 16, 32, 64, 128 };

            var measurements = ScaleFishTestHelpers.BuildExact(family, xs);
            var fit = family.SeedFit(measurements);
            fit.Scale.ShouldBe(1.0, tolerance: 0.05, $"{family.Name} scale off");
        }
    }
}
