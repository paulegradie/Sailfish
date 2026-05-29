using System;
using System.Linq;
using Sailfish.Analysis.ScaleFish;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// The parallel and serial bootstrap modes must produce bit-for-bit identical results for the same input,
/// because each iteration's RNG is seeded from (data-hash, iteration-index) — independent of execution order.
/// </summary>
public class ScaleFishParallelBootstrapTests
{
    [Fact]
    public void ParallelAndSerial_ProduceIdenticalDiagnostic()
    {
        var rng = new Random(11);
        var measurements = ScaleFishTestHelpers.BuildNoisy(
            x => x * x,
            ScaleFishTestHelpers.LogSpacedX(8, 256, 6),
            sampleSize: 30,
            relativeNoise: 0.05,
            rng);

        // Snapshot the data: parallel run mutates nothing, but make explicit clones so the determinism
        // test isn't contaminated by shared array references.
        var clone = measurements
            .Select(m => new ComplexityMeasurement(m.X, m.Y, m.StdDev, m.SampleSize, (double[])m.RawSamples!.Clone()))
            .ToArray();

        var serial = new ComplexityEstimator(new ScaleFishSettings { EnableParallelBootstrap = false }).EstimateComplexity(measurements);
        var parallel = new ComplexityEstimator(new ScaleFishSettings { EnableParallelBootstrap = true }).EstimateComplexity(clone);

        serial.ShouldNotBeNull();
        parallel.ShouldNotBeNull();
        serial.Bootstrap.ShouldNotBeNull();
        parallel.Bootstrap.ShouldNotBeNull();

        serial.Bootstrap.Iterations.ShouldBe(parallel.Bootstrap.Iterations);
        serial.Bootstrap.SelectionAgreement.ShouldBe(parallel.Bootstrap.SelectionAgreement);
        serial.Bootstrap.ScaleCiLower.ShouldBe(parallel.Bootstrap.ScaleCiLower);
        serial.Bootstrap.ScaleCiUpper.ShouldBe(parallel.Bootstrap.ScaleCiUpper);
        serial.Bootstrap.BiasCiLower.ShouldBe(parallel.Bootstrap.BiasCiLower);
        serial.Bootstrap.BiasCiUpper.ShouldBe(parallel.Bootstrap.BiasCiUpper);
    }

    [Fact]
    public void RepeatedParallelRuns_AreIdentical()
    {
        var rng = new Random(13);
        var src = ScaleFishTestHelpers.BuildNoisy(
            x => x,
            ScaleFishTestHelpers.LogSpacedX(8, 512, 6),
            sampleSize: 25,
            relativeNoise: 0.05,
            rng);

        // Three independent runs with deep-copied data — same input ⇒ same output.
        var settings = new ScaleFishSettings { EnableParallelBootstrap = true };
        var results = Enumerable.Range(0, 3).Select(_ =>
        {
            var copy = src.Select(m => new ComplexityMeasurement(m.X, m.Y, m.StdDev, m.SampleSize, (double[])m.RawSamples!.Clone())).ToArray();
            return new ComplexityEstimator(settings).EstimateComplexity(copy)!.Bootstrap!;
        }).ToArray();

        for (var i = 1; i < results.Length; i++)
        {
            results[i].SelectionAgreement.ShouldBe(results[0].SelectionAgreement);
            results[i].ScaleCiLower.ShouldBe(results[0].ScaleCiLower);
            results[i].ScaleCiUpper.ShouldBe(results[0].ScaleCiUpper);
        }
    }
}
