using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;
using System.Collections.Generic;
using System.Threading;

namespace PerformanceTests.ExamplePerformanceTests;

/// <summary>
/// Example showing the new ISailfishComplexVariableProvider system working alongside
/// traditional SailfishVariableAttribute for different types of test parameters
/// </summary>
[Sailfish(NumWarmupIterations = 1, SampleSize = 3)]
public class ComplexVariableProviderExample
{
    // Traditional attribute-based variable for simple types
    [SailfishVariable(10, 100, 1000)]
    public int BufferSize { get; set; }

    // New interface-based variable for complex objects
    public ITestConfiguration Configuration { get; set; } = null!;

    [SailfishMethod]
    public void ProcessData()
    {
        // Simulate processing with different buffer sizes and configurations
        var buffer = new byte[BufferSize];
        
        // Use the complex configuration object
        var processingTime = Configuration.ProcessingDelay;
        var algorithm = Configuration.Algorithm;
        var settings = Configuration.Settings;
        
        // Simulate work
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = (byte)(i % 256);
        }
        
        // Simulate algorithm-specific processing
        if (algorithm == ProcessingAlgorithm.Fast)
        {
            Thread.Sleep(processingTime / 2);
        }
        else
        {
            Thread.Sleep(processingTime);
        }
    }
}

/// <summary>
/// Interface for complex test configuration that implements ISailfishComplexVariableProvider
/// This allows the property to serve dual purposes:
/// 1. Hold the iteration data for the performance test
/// 2. Provide instances of complex objects at runtime
/// </summary>
public interface ITestConfiguration : ISailfishComplexVariableProvider<TestConfiguration>
{
    ProcessingAlgorithm Algorithm { get; }
    int ProcessingDelay { get; }
    Dictionary<string, object> Settings { get; }
}

/// <summary>
/// Implementation of the complex variable provider
/// </summary>
public record TestConfiguration(
    ProcessingAlgorithm Algorithm,
    int ProcessingDelay,
    Dictionary<string, object> Settings) : ITestConfiguration
{
    public int CompareTo(object? obj)
    {
        if (obj is not TestConfiguration other) return 1;
        
        var algorithmComparison = Algorithm.CompareTo(other.Algorithm);
        if (algorithmComparison != 0) return algorithmComparison;
        
        return ProcessingDelay.CompareTo(other.ProcessingDelay);
    }

    /// <summary>
    /// Static method that provides the variable instances for testing
    /// Each instance returned will result in a separate test execution
    /// </summary>
    public static IEnumerable<TestConfiguration> GetVariableInstances()
    {
        return new[]
        {
            new TestConfiguration(
                ProcessingAlgorithm.Fast,
                10,
                new Dictionary<string, object> { ["CacheEnabled"] = true, ["MaxRetries"] = 3 }
            ),
            new TestConfiguration(
                ProcessingAlgorithm.Thorough,
                50,
                new Dictionary<string, object> { ["CacheEnabled"] = false, ["MaxRetries"] = 5 }
            ),
            new TestConfiguration(
                ProcessingAlgorithm.Balanced,
                25,
                new Dictionary<string, object> { ["CacheEnabled"] = true, ["MaxRetries"] = 2 }
            )
        };
    }
}

public enum ProcessingAlgorithm
{
    Fast,
    Balanced,
    Thorough
}
