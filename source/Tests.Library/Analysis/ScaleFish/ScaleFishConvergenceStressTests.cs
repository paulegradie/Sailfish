using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Analysis.ScaleFish.ComplexityFunctions;
using Shouldly;
using Xunit;

namespace Tests.Library.Analysis.ScaleFish;

/// <summary>
/// CI stability stress tests: each family runs across many deterministic seeds with realistic noise.
/// Designed to catch flaky/probabilistic convergence behaviour that might pass a small-seed run.
/// </summary>
public class ScaleFishConvergenceStressTests
{
    [Theory]
    [InlineData(typeof(Linear),      8, 1024,   25,  0.05)]
    [InlineData(typeof(NLogN),       8, 1024,   25,  0.05)]
    [InlineData(typeof(Quadratic),   4, 256,    25,  0.05)]
    [InlineData(typeof(Cubic),       4, 128,    25,  0.05)]
    [InlineData(typeof(SqrtN),       4, 4096,   25,  0.05)]
    [InlineData(typeof(Logarithmic), 4, 4096,   25,  0.05)]
    public void EveryFamily_ConvergesAcross25Seeds(Type familyType, int minX, int maxX, int seeds, double noise)
    {
        var instance = (ScaleFishModelFunction)Activator.CreateInstance(familyType)!;
        var xs = ScaleFishTestHelpers.LogSpacedX(minX, maxX, 6);
        var estimator = new ComplexityEstimator();

        var winnerCounts = new Dictionary<string, int>();
        for (var seed = 1; seed <= seeds; seed++)
        {
            var rng = new Random(seed);
            var measurements = ScaleFishTestHelpers.BuildNoisy(
                x => instance.Compute(0.0, 1.0, x),
                xs,
                sampleSize: 30,
                relativeNoise: noise,
                rng);
            var result = estimator.EstimateComplexity(measurements);
            result.ShouldNotBeNull();
            winnerCounts.TryGetValue(result.ScaleFishModelFunction.Name, out var current);
            winnerCounts[result.ScaleFishModelFunction.Name] = current + 1;
        }

        // Exact-name expectation: the LogLinear clone is deserialization-only, so each family's data
        // has exactly one correct winner.
        var expected = familyType.Name;
        winnerCounts.TryGetValue(expected, out var wins);
        wins.ShouldBe(
            seeds,
            $"{expected}: expected {seeds}/{seeds}, got: {string.Join(", ", winnerCounts.Select(kv => $"{kv.Key}={kv.Value}"))}");
    }
}
