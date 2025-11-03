using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace AdaptiveSamplingTest;

/// <summary>
/// Simple test to demonstrate adaptive sampling functionality.
/// This test should converge quickly due to consistent timing.
/// </summary>
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05, MaximumSampleSize = 50)]
public class SimpleAdaptiveSamplingTest
{
    [SailfishMethod]
    public async Task ConsistentMethod(CancellationToken cancellationToken)
    {
        // Very consistent operation - should converge quickly
        await Task.Delay(10, cancellationToken);
    }
}

/// <summary>
/// Test with traditional fixed sampling for comparison.
/// </summary>
[Sailfish(UseAdaptiveSampling = false, SampleSize = 20)]
public class FixedSamplingTest
{
    [SailfishMethod]
    public async Task FixedMethod(CancellationToken cancellationToken)
    {
        // Same operation but with fixed sampling
        await Task.Delay(10, cancellationToken);
    }
}
