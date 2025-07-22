using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;
using System;
using System.Collections.Generic;

namespace PerformanceTests.ExamplePerformanceTests;

/// <summary>
/// Comprehensive demonstration showing ALL variable provider approaches working together:
/// 1. Simple attribute-based variables (original system)
/// 2. Type-based attribute variables (original system)
/// 3. Range-based attribute variables (original system)
/// 4. New interface-based typed variables (new system with better separation of concerns)
///
/// This shows complete backward compatibility while adding new capabilities.
/// The new ISailfishVariables&lt;VariableType, VariableTypeProvider&gt; pattern separates data concerns from variable generation.
/// </summary>
[Sailfish(NumWarmupIterations = 1, SampleSize = 2)]
public class BackwardCompatibilityExample
{
    // 1. ORIGINAL: Simple attribute-based variables (still works exactly as before)
    [SailfishVariable(10, 50, 100)]
    public int SimpleNumbers { get; set; }

    [SailfishVariable("Alpha", "Beta", "Gamma")]
    public string SimpleStrings { get; set; } = null!;

    // 2. ORIGINAL: Type-based attribute variables (still works exactly as before)
    [SailfishVariable(typeof(CustomStringProvider))]
    public string TypeBasedString { get; set; } = null!;

    [SailfishVariable(typeof(CustomObjectProvider))]
    public CustomObject TypeBasedObject { get; set; } = null!;

    // 3. ORIGINAL: Range-based variables (still works exactly as before)
    [SailfishRangeVariable(1, 5, 2)]
    public int RangeNumbers { get; set; }

    // 4. NEW: Interface-based typed variables (new feature with better separation of concerns)
    public IAlgorithmConfiguration AlgorithmConfig { get; set; } = null!;

    [SailfishMethod]
    public void DemoAllVariableTypes()
    {
        // All variable types work together seamlessly
        Console.WriteLine($"Simple: {SimpleNumbers}, {SimpleStrings}");
        Console.WriteLine($"Type-based: {TypeBasedString}, {TypeBasedObject.Name}");
        Console.WriteLine($"Range: {RangeNumbers}");
        Console.WriteLine($"Algorithm: {AlgorithmConfig.Algorithm}, {AlgorithmConfig.Settings.Count}");

        // Simulate some work
        System.Threading.Thread.Sleep(1);
    }
}

// ORIGINAL: Type-based providers (still work exactly as before)
public class CustomStringProvider : ISailfishVariablesProvider<string>
{
    public IEnumerable<string> Variables()
    {
        return new[] { "Provider1", "Provider2", "Provider3" };
    }
}

public class CustomObjectProvider : ISailfishVariablesProvider<CustomObject>
{
    public IEnumerable<CustomObject> Variables()
    {
        return new[]
        {
            new CustomObject("Object1", 100),
            new CustomObject("Object2", 200),
            new CustomObject("Object3", 300)
        };
    }
}

public record CustomObject(string Name, int Value) : IComparable
{
    public int CompareTo(object? obj)
    {
        if (obj is not CustomObject other) return 1;
        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }
}

// NEW: Interface-based typed variables with better separation of concerns
public interface IAlgorithmConfiguration : ISailfishVariables<AlgorithmConfiguration, AlgorithmConfigurationProvider>
{
    string Algorithm { get; }
    Dictionary<string, object> Settings { get; }
    TimeSpan Timeout { get; }
}

// Separate provider class handles variable generation
public class AlgorithmConfigurationProvider : ISailfishVariablesProvider<AlgorithmConfiguration>
{
    public IEnumerable<AlgorithmConfiguration> Variables()
    {
        return new[]
        {
            new AlgorithmConfiguration(
                "FastAlgorithm",
                new Dictionary<string, object> { ["CacheSize"] = 1000, ["Parallel"] = true },
                TimeSpan.FromSeconds(5)
            ),
            new AlgorithmConfiguration(
                "AccurateAlgorithm",
                new Dictionary<string, object> { ["CacheSize"] = 5000, ["Parallel"] = false },
                TimeSpan.FromSeconds(30)
            ),
            new AlgorithmConfiguration(
                "BalancedAlgorithm",
                new Dictionary<string, object> { ["CacheSize"] = 2500, ["Parallel"] = true },
                TimeSpan.FromSeconds(15)
            )
        };
    }
}

// Clean data type - no variable generation concerns
public record AlgorithmConfiguration(
    string Algorithm,
    Dictionary<string, object> Settings,
    TimeSpan Timeout) : IAlgorithmConfiguration
{
    public int CompareTo(object? obj)
    {
        if (obj is not AlgorithmConfiguration other) return 1;

        var algorithmComparison = string.Compare(Algorithm, other.Algorithm, StringComparison.Ordinal);
        if (algorithmComparison != 0) return algorithmComparison;

        return Timeout.CompareTo(other.Timeout);
    }

    // No GetVariableInstances() method needed - the provider handles that!
}
