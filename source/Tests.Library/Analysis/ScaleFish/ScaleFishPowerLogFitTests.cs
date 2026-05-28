using System;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Sailfish.Analysis.ScaleFish.CurveFitting;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Verifies the continuous power-log diagnostic produces (b, c) close to the textbook reference points
/// for known families, and that it picks the nearest discrete family correctly.
/// </summary>
public class ScaleFishPowerLogFitTests
{
    private const double ExponentTolerance = 0.15;

    [Fact]
    public void NoisyLinear_PowerLog_GivesBNear1AndCNear0()
    {
        var rng = new Random(1);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => x,
            ScaleFishTestHelpers.LogSpacedX(4, 1024, 8),
            sampleSize: 50,
            relativeNoise: 0.03,
            rng);

        var fit = PowerLogFit.TryFit(measurements);
        fit.ShouldNotBeNull();
        fit.B.ShouldBe(1.0, tolerance: ExponentTolerance);
        fit.C.ShouldBe(0.0, tolerance: ExponentTolerance);
        fit.NearestDiscreteFamily().ShouldBe(nameof(Linear));
    }

    [Fact]
    public void NoisyQuadratic_PowerLog_GivesBNear2AndCNear0()
    {
        var rng = new Random(2);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => x * x,
            ScaleFishTestHelpers.LogSpacedX(4, 512, 8),
            sampleSize: 50,
            relativeNoise: 0.03,
            rng);

        var fit = PowerLogFit.TryFit(measurements);
        fit.ShouldNotBeNull();
        fit.B.ShouldBe(2.0, tolerance: ExponentTolerance);
        fit.C.ShouldBe(0.0, tolerance: ExponentTolerance);
        fit.NearestDiscreteFamily().ShouldBe(nameof(Quadratic));
    }

    [Fact]
    public void NoisyCubic_PowerLog_GivesBNear3AndCNear0()
    {
        var rng = new Random(3);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => x * x * x,
            ScaleFishTestHelpers.LogSpacedX(4, 256, 8),
            sampleSize: 50,
            relativeNoise: 0.03,
            rng);

        var fit = PowerLogFit.TryFit(measurements);
        fit.ShouldNotBeNull();
        fit.B.ShouldBe(3.0, tolerance: ExponentTolerance);
        fit.C.ShouldBe(0.0, tolerance: ExponentTolerance);
        fit.NearestDiscreteFamily().ShouldBe(nameof(Cubic));
    }

    [Fact]
    public void NoisySqrtN_PowerLog_GivesBNearHalfAndCNear0()
    {
        var rng = new Random(4);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => Math.Sqrt(x),
            ScaleFishTestHelpers.LogSpacedX(4, 4096, 8),
            sampleSize: 50,
            relativeNoise: 0.03,
            rng);

        var fit = PowerLogFit.TryFit(measurements);
        fit.ShouldNotBeNull();
        fit.B.ShouldBe(0.5, tolerance: ExponentTolerance);
        fit.NearestDiscreteFamily().ShouldBe(nameof(SqrtN));
    }

    [Fact]
    public void TooFewPoints_ReturnsNull()
    {
        // PowerLog needs ≥ 4 points with x > 1.
        var measurements = new[]
        {
            new ComplexityMeasurement(2, 4),
            new ComplexityMeasurement(4, 16)
        };
        PowerLogFit.TryFit(measurements).ShouldBeNull();
    }

    [Fact]
    public void NonPositiveY_FilteredOut()
    {
        // One non-positive Y plus four valid quadratic points — filtering should drop the bad point
        // and recover the quadratic exponent from the remaining sample.
        var measurements = new[]
        {
            new ComplexityMeasurement(2, -1),     // filtered: Y ≤ 0
            new ComplexityMeasurement(4, 16),
            new ComplexityMeasurement(8, 64),
            new ComplexityMeasurement(16, 256),
            new ComplexityMeasurement(32, 1024),
            new ComplexityMeasurement(64, 4096)
        };
        var fit = PowerLogFit.TryFit(measurements);
        fit.ShouldNotBeNull();
        fit.B.ShouldBe(2.0, tolerance: 0.1);
    }
}
