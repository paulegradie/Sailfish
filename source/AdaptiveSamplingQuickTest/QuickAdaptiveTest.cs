using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace AdaptiveSamplingQuickTest;

/// <summary>
/// Quick test to verify adaptive sampling is working.
/// This test should converge quickly due to consistent timing.
/// </summary>
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05, MaximumSampleSize = 50)]
public class QuickAdaptiveTest
{
    [SailfishMethod]
    public async Task ConsistentMethod(CancellationToken cancellationToken)
    {
        // Very consistent operation - should converge quickly
        await Task.Delay(5, cancellationToken);
    }
}

/// <summary>
/// Traditional fixed sampling test for comparison.
/// </summary>
[Sailfish(UseAdaptiveSampling = false, SampleSize = 10)]
public class QuickFixedTest
{
    [SailfishMethod]
    public async Task FixedMethod(CancellationToken cancellationToken)
    {
        // Same operation but with fixed sampling
        await Task.Delay(5, cancellationToken);
    }
}
