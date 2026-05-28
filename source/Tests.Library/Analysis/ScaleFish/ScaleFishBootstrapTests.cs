using System;
using System.Linq;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Verifies the bootstrap diagnostic engages when raw replicates are present, is deterministic for
/// identical inputs, and reports high selection agreement for clearly classifiable data.
/// </summary>
public class ScaleFishBootstrapTests
{
    [Fact]
    public void BootstrapOmitted_WhenRawSamplesMissing()
    {
        var quadratic = new Quadratic();
        var measurements = ScaleFishTestHelpers.BuildExact(quadratic, new[] { 4, 8, 16, 32, 64 });
        var result = new ComplexityEstimator().EstimateComplexity(measurements);

        result.ShouldNotBeNull();
        result.Bootstrap.ShouldBeNull("synthetic exact measurements carry no raw replicates");
    }

    [Fact]
    public void Bootstrap_RunsWhenRawSamplesPresent()
    {
        var rng = new Random(11);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => x,
            ScaleFishTestHelpers.LogSpacedX(8, 512, 6),
            sampleSize: 30,
            relativeNoise: 0.04,
            rng);

        var result = new ComplexityEstimator().EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.Bootstrap.ShouldNotBeNull();
        result.Bootstrap.Iterations.ShouldBe(ComplexityEstimator.DefaultBootstrapIterations);
    }

    [Fact]
    public void Bootstrap_IsDeterministicForIdenticalInputs()
    {
        var rng1 = new Random(13);
        var m1 = ScaleFishTestHelpers.BuildNoisy(
            x => x * x,
            ScaleFishTestHelpers.LogSpacedX(8, 256, 6),
            sampleSize: 25,
            relativeNoise: 0.05,
            rng1);

        // Deep-copy the measurements so the second run gets the same raw samples.
        var m2 = m1
            .Select(m => new ComplexityMeasurement(m.X, m.Y, m.StdDev, m.SampleSize, (double[])m.RawSamples!.Clone()))
            .ToArray();

        var r1 = new ComplexityEstimator().EstimateComplexity(m1);
        var r2 = new ComplexityEstimator().EstimateComplexity(m2);

        r1.ShouldNotBeNull();
        r2.ShouldNotBeNull();
        r1.Bootstrap.ShouldNotBeNull();
        r2.Bootstrap.ShouldNotBeNull();
        r1.Bootstrap.SelectionAgreement.ShouldBe(r2.Bootstrap.SelectionAgreement);
        r1.Bootstrap.ScaleCiLower.ShouldBe(r2.Bootstrap.ScaleCiLower);
        r1.Bootstrap.ScaleCiUpper.ShouldBe(r2.Bootstrap.ScaleCiUpper);
    }

    [Fact]
    public void Bootstrap_HighAgreementForClearCase()
    {
        // Quadratic with low noise across a wide log range — bootstrap should rarely disagree.
        var rng = new Random(17);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => x * x,
            ScaleFishTestHelpers.LogSpacedX(8, 512, 6),
            sampleSize: 40,
            relativeNoise: 0.03,
            rng);

        var result = new ComplexityEstimator().EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.ScaleFishModelFunction.Name.ShouldBe(nameof(Quadratic));
        result.Bootstrap.ShouldNotBeNull();
        result.Bootstrap.SelectionAgreement.ShouldBeGreaterThan(0.95);
    }

    [Fact]
    public void Bootstrap_CIBracketsTrueScale()
    {
        // True linear with scale=2.5; the 95% CI should contain it.
        const double trueScale = 2.5;
        var rng = new Random(19);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => trueScale * x,
            ScaleFishTestHelpers.LogSpacedX(16, 1024, 6),
            sampleSize: 40,
            relativeNoise: 0.04,
            rng);

        var result = new ComplexityEstimator().EstimateComplexity(measurements);
        result.ShouldNotBeNull();
        result.Bootstrap.ShouldNotBeNull();
        result.Bootstrap.ScaleCiLower.ShouldBeLessThan(trueScale);
        result.Bootstrap.ScaleCiUpper.ShouldBeGreaterThan(trueScale);
    }
}
