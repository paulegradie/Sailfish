using System;
using System.Linq;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// Verifies that ScaleFish converges to the correct family even with realistic measurement noise.
/// Each test runs the estimator with multiple deterministic seeds to ensure convergence is consistent
/// across runs — required for CI stability.
/// </summary>
public class ScaleFishNoisyConvergenceTests
{
    private const int Seeds = 10;
    private const int SampleSize = 30;
    private const double RelativeNoise = 0.05;

    [Fact]
    public void Noisy_Linear_LogSpaced_ConvergesEverySeed()
    {
        AssertConverges<Linear>(xs: ScaleFishTestHelpers.LogSpacedX(8, 512, 6));
    }

    [Fact]
    public void Noisy_NLogN_LogSpaced_ConvergesEverySeed()
    {
        AssertConverges<NLogN>(xs: ScaleFishTestHelpers.LogSpacedX(8, 512, 6));
    }

    [Fact]
    public void Noisy_Quadratic_LogSpaced_ConvergesEverySeed()
    {
        AssertConverges<Quadratic>(xs: ScaleFishTestHelpers.LogSpacedX(4, 256, 6));
    }

    [Fact]
    public void Noisy_Cubic_LogSpaced_ConvergesEverySeed()
    {
        AssertConverges<Cubic>(xs: ScaleFishTestHelpers.LogSpacedX(4, 128, 6));
    }

    [Fact]
    public void Noisy_SqrtN_LogSpaced_ConvergesEverySeed()
    {
        AssertConverges<SqrtN>(xs: ScaleFishTestHelpers.LogSpacedX(4, 1024, 6));
    }

    [Fact]
    public void FewerPoints_LogSpaced_LinearStillConverges()
    {
        // 4 well-chosen log-spaced X values are enough to distinguish Linear from the alternatives.
        AssertConverges<Linear>(xs: ScaleFishTestHelpers.LogSpacedX(16, 1024, 4));
    }

    [Fact]
    public void FewerPoints_LogSpaced_QuadraticStillConverges()
    {
        AssertConverges<Quadratic>(xs: ScaleFishTestHelpers.LogSpacedX(8, 512, 4));
    }

    [Fact]
    public void LinearSpaced_Linear_ConvergesEverySeed()
    {
        // Same family, but with linear spacing — should still converge though less efficient.
        AssertConverges<Linear>(xs: new[] { 10, 30, 50, 80, 130, 200 });
    }

    [Fact]
    public void LinearSpaced_Quadratic_ConvergesEverySeed()
    {
        AssertConverges<Quadratic>(xs: new[] { 10, 30, 50, 80, 130, 200 });
    }

    private static void AssertConverges<TFamily>(int[] xs) where TFamily : ScaleFishModelFunction
    {
        var instance = ScaleFishTestHelpers.CreateFamily<TFamily>();
        var estimator = new ComplexityEstimator();
        var winnerCounts = new System.Collections.Generic.Dictionary<string, int>();

        for (var seed = 1; seed <= Seeds; seed++)
        {
            var rng = new Random(seed);
            var measurements = ScaleFishTestHelpers.BuildNoisy(
                trueFunction: x => instance.Compute(0.0, 1.0, x),
                xs: xs,
                sampleSize: SampleSize,
                relativeNoise: RelativeNoise,
                rng: rng);
            var result = estimator.EstimateComplexity(measurements);
            result.ShouldNotBeNull($"Seed {seed} produced no result");
            var name = result.ScaleFishModelFunction.Name;
            winnerCounts.TryGetValue(name, out var current);
            winnerCounts[name] = current + 1;
        }

        // Exact-name expectation: the LogLinear clone (x·log₂x, collinear with NLogN's x·ln x) is
        // deserialization-only now, so n·log n data has exactly one correct winner.
        var expected = typeof(TFamily).Name;
        winnerCounts.TryGetValue(expected, out var wins);

        wins.ShouldBe(
            Seeds,
            $"Expected {expected} to win every seed, got: {string.Join(", ", winnerCounts.Select(kv => $"{kv.Key}={kv.Value}"))}");
    }
}
