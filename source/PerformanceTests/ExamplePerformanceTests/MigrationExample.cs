using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;
using System;
using System.Collections.Generic;

namespace PerformanceTests.ExamplePerformanceTests;

/// <summary>
/// Example showing how to migrate from the old Type-based system to the new interface-based system
/// </summary>

// OLD WAY: Using SailfishVariableAttribute with Type parameter
[Sailfish(NumWarmupIterations = 1, SampleSize = 2, Disabled = true)]
public class OldWayExample
{
    [SailfishVariable(typeof(OldStyleProvider))]
    public string TestValue { get; set; } = null!;

    [SailfishMethod]
    public void TestMethod()
    {
        Console.WriteLine($"Processing: {TestValue}");
    }
}

// Old style provider - still works for backward compatibility
public class OldStyleProvider : ISailfishVariablesProvider<string>
{
    public IEnumerable<string> Variables()
    {
        return new[] { "Value1", "Value2", "Value3" };
    }
}

// NEW WAY: Using ISailfishComplexVariableProvider interface
[Sailfish(NumWarmupIterations = 1, SampleSize = 2)]
public class NewWayExample
{
    // Property type implements ISailfishComplexVariableProvider directly
    public ITestData TestData { get; set; } = null!;

    [SailfishMethod]
    public void TestMethod()
    {
        Console.WriteLine($"Processing: {TestData.Name} with value {TestData.Value}");
    }
}

// New style interface - cleaner and more type-safe
public interface ITestData : ISailfishComplexVariableProvider<TestData>
{
    string Name { get; }
    int Value { get; }
}

// Implementation provides the variable instances
public record TestData(string Name, int Value) : ITestData
{
    public int CompareTo(object? obj)
    {
        if (obj is not TestData other) return 1;
        
        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0) return nameComparison;
        
        return Value.CompareTo(other.Value);
    }

    public static IEnumerable<TestData> GetVariableInstances()
    {
        return new[]
        {
            new TestData("Data1", 100),
            new TestData("Data2", 200),
            new TestData("Data3", 300)
        };
    }
}

/// <summary>
/// Benefits of the new system:
/// 
/// 1. Type Safety: The property type directly implements the interface, providing compile-time type checking
/// 2. Cleaner API: No need to pass Type parameters to attributes
/// 3. Better IntelliSense: IDE can provide better code completion and navigation
/// 4. Self-Documenting: The interface clearly shows what the property expects
/// 5. Easier Testing: You can easily create test instances without reflection
/// 6. Backward Compatible: Old attribute-based system continues to work
/// 
/// Migration Steps:
/// 1. Create an interface that extends ISailfishComplexVariableProvider&lt;T&gt;
/// 2. Define your data properties on the interface
/// 3. Create a record/class that implements the interface
/// 4. Implement CompareTo for ordering (required by IComparable)
/// 5. Implement GetVariableInstances() to return your test data
/// 6. Change your test property type to use the interface
/// 7. Remove the [SailfishVariable(typeof(...))] attribute
/// </summary>
public static class MigrationGuide
{
    // This class exists only for documentation purposes
}
