using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.AdaptiveSamplingDemos;

/// <summary>
/// Demo test class showcasing adaptive sampling with consistent performance.
/// This test should converge quickly due to low variability.
/// </summary>
[WriteToMarkdown]
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05, MaximumSampleSize = 100)]
public class ConsistentPerformanceDemo
{
    [SailfishMethod]
    public async Task ConsistentOperation(CancellationToken cancellationToken)
    {
        // Simulate a very consistent operation (always ~10ms)
        await Task.Delay(10, cancellationToken);
    }

    [SailfishMethod]
    public async Task SlightlyVariableOperation(CancellationToken cancellationToken)
    {
        // Simulate slight variability (9-11ms)
        var random = new Random();
        var delay = 9 + random.Next(3); // 9, 10, or 11ms
        await Task.Delay(delay, cancellationToken);
    }
}

/// <summary>
/// Demo test class showcasing adaptive sampling with variable performance.
/// This test should reach maximum iterations due to high variability.
/// </summary>
[WriteToMarkdown]
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.25, MaximumSampleSize = 50)]
public class VariablePerformanceDemo
{
    private static int counter = 0;

    [SailfishMethod]
    public async Task HighlyVariableOperation(CancellationToken cancellationToken)
    {
        // Simulate high variability (0-50ms)
        var delay = (counter++ % 11) * 5; // 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50ms
        await Task.Delay(delay, cancellationToken);
    }

    [SailfishMethod]
    public async Task RandomVariableOperation(CancellationToken cancellationToken)
    {
        // Simulate random variability (1-100ms)
        var random = new Random();
        var delay = random.Next(1, 101);
        await Task.Delay(delay, cancellationToken);
    }
}

/// <summary>
/// Demo test class comparing adaptive vs fixed sampling approaches.
/// Shows the difference between traditional fixed sampling and adaptive sampling.
/// </summary>
[WriteToMarkdown]
[Sailfish(UseAdaptiveSampling = false, SampleSize = 20)] // Fixed sampling for comparison
public class FixedSamplingComparison
{
    [SailfishMethod]
    public async Task FixedSamplingConsistentOperation(CancellationToken cancellationToken)
    {
        // Same consistent operation as adaptive demo
        await Task.Delay(10, cancellationToken);
    }

    [SailfishMethod] 
    public async Task FixedSamplingVariableOperation(CancellationToken cancellationToken)
    {
        // Same variable operation as adaptive demo
        var random = new Random();
        var delay = random.Next(1, 101);
        await Task.Delay(delay, cancellationToken);
    }
}

/// <summary>
/// Demo test class showcasing custom adaptive sampling configuration.
/// Uses stricter convergence criteria and higher maximum iterations.
/// </summary>
[WriteToMarkdown]
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.02, MaximumSampleSize = 200)]
public class StrictConvergenceDemo
{
    [SailfishMethod]
    public async Task StrictConvergenceOperation(CancellationToken cancellationToken)
    {
        // Simulate moderate variability that requires strict convergence
        var random = new Random();
        var baseDelay = 50;
        var variation = random.Next(-5, 6); // ±5ms variation
        await Task.Delay(baseDelay + variation, cancellationToken);
    }
}

/// <summary>
/// Demo test class showcasing adaptive sampling with different workload types.
/// Demonstrates how adaptive sampling handles various performance patterns.
/// </summary>
[WriteToMarkdown]
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.05, MaximumSampleSize = 150)]
public class WorkloadPatternsDemo
{
    private static int cpuIntensiveCounter = 0;

    [SailfishMethod]
    public async Task CpuIntensiveOperation(CancellationToken cancellationToken)
    {
        // Simulate CPU-intensive work with consistent timing
        var iterations = 100000;
        var sum = 0;
        for (int i = 0; i < iterations; i++)
        {
            sum += i * 2;
            if (i % 10000 == 0 && cancellationToken.IsCancellationRequested)
                break;
        }
        await Task.Yield(); // Yield to prevent compiler optimization
    }

    [SailfishMethod]
    public async Task IoSimulationOperation(CancellationToken cancellationToken)
    {
        // Simulate I/O operation with some variability
        var random = new Random();
        var delay = 20 + random.Next(-3, 4); // 17-23ms
        await Task.Delay(delay, cancellationToken);
    }

    [SailfishMethod]
    public async Task MemoryAllocationOperation(CancellationToken cancellationToken)
    {
        // Simulate memory allocation patterns
        var arrays = new int[10][];
        for (int i = 0; i < 10; i++)
        {
            arrays[i] = new int[1000];
            Array.Fill(arrays[i], i);
        }
        
        // Small delay to make timing measurable
        await Task.Delay(1, cancellationToken);
        
        // Force garbage collection to add some variability
        if (cpuIntensiveCounter++ % 5 == 0)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}

/// <summary>
/// Demo test class showcasing edge cases for adaptive sampling.
/// Tests behavior with very fast operations and extreme scenarios.
/// </summary>
[WriteToMarkdown]
[Sailfish(UseAdaptiveSampling = true, TargetCoefficientOfVariation = 0.1, MaximumSampleSize = 300)]
public class EdgeCasesDemo
{
    [SailfishMethod]
    public async Task VeryFastOperation(CancellationToken cancellationToken)
    {
        // Very fast operation - tests adaptive sampling with microsecond timings
        await Task.Yield();
    }

    [SailfishMethod]
    public async Task ZeroDelayOperation(CancellationToken cancellationToken)
    {
        // Essentially no delay - tests edge case handling
        await Task.CompletedTask;
    }

    [SailfishMethod]
    public async Task OccasionalSpike(CancellationToken cancellationToken)
    {
        // Usually fast, but occasional spikes
        var random = new Random();
        if (random.Next(100) < 5) // 5% chance of spike
        {
            await Task.Delay(50, cancellationToken); // Spike
        }
        else
        {
            await Task.Delay(1, cancellationToken); // Normal
        }
    }
}
