using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;
using System;
using System.Collections.Generic;

namespace Tests.E2E.TestSuite.Discoverable;

/// <summary>
/// End-to-end integration test for the new ISailfishVariables&lt;VariableType, VariableTypeProvider&gt; pattern.
/// This test verifies that the complete pipeline works from discovery through execution.
/// </summary>
[Sailfish(NumWarmupIterations = 1, SampleSize = 2)]
public class ComplexVariablesIntegrationTest
{
    // Traditional attribute-based variable
    [SailfishVariable(10, 20)]
    public int BufferSize { get; set; }

    // New ISailfishVariables pattern
    public ITestConfiguration Configuration { get; set; } = null!;

    [SailfishMethod]
    public void TestComplexVariables()
    {
        // Verify that both variable types are properly set
        if (BufferSize <= 0)
            throw new InvalidOperationException("BufferSize should be set by Sailfish");

        if (Configuration == null)
            throw new InvalidOperationException("Configuration should be set by Sailfish");

        if (string.IsNullOrEmpty(Configuration.Name))
            throw new InvalidOperationException("Configuration.Name should be set");

        if (Configuration.Timeout <= 0)
            throw new InvalidOperationException("Configuration.Timeout should be set");

        // Simulate some work
        Console.WriteLine($"Testing with buffer: {BufferSize}, config: {Configuration.Name}, timeout: {Configuration.Timeout}");
        System.Threading.Thread.Sleep(1);
    }
}

// Test configuration interface using the new pattern
public interface ITestConfiguration : ISailfishVariables<TestConfiguration, TestConfigurationProvider>
{
    string Name { get; }
    int Timeout { get; }
    bool IsEnabled { get; }
}

// Provider for test configurations
public class TestConfigurationProvider : ISailfishVariablesProvider<TestConfiguration>
{
    public IEnumerable<TestConfiguration> Variables()
    {
        return new[]
        {
            new TestConfiguration("FastConfig", 5, true),
            new TestConfiguration("SlowConfig", 30, false)
        };
    }
}

// Data type for test configurations
public record TestConfiguration(string Name, int Timeout, bool IsEnabled) : ITestConfiguration
{
    public int CompareTo(object? obj)
    {
        if (obj is not TestConfiguration other) return 1;

        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0) return nameComparison;

        return Timeout.CompareTo(other.Timeout);
    }
}
