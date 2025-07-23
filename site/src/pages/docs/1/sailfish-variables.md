---
title: Sailfish Variables
---

**Sailfish variables** allow you to create multiple test cases with different test class states. Sailfish supports several approaches to defining variables, from simple attribute-based variables to complex object instances with sophisticated provider patterns.

## Overview

Sailfish provides three main approaches for defining test variables:

1. **Simple Variables** - Using attributes for basic types (`[SailfishVariable]`, `[SailfishRangeVariable]`)
2. **Complex Variables** - Using interfaces or classes for complex object instances
3. **Legacy Complex Variables** - Older patterns maintained for backward compatibility

---

## Simple Variables

Simple variables are perfect for basic types like integers, strings, enums, and other primitive values.

### SailfishVariable Attribute

```csharp
[Sailfish]
public class Example
{
    [SailfishVariable(10, 100, 1000)] // params object[]
    public int SleepPeriod { get; set; }

    [SailfishMethod]
    public void Method()
    {
        Thread.Sleep(SleepPeriod);
    }
}
```

### SailfishRangeVariable Attribute

```csharp
[Sailfish]
public class Example
{
    [SailfishRangeVariable(start: 1, count: 3, step: 100)]
    public int SleepPeriod { get; set; }

    [SailfishMethod]
    public void Method()
    {
        Thread.Sleep(SleepPeriod);
    }
}
```

#### Parameters:
- **start**: The starting number for the range
- **count**: The number of elements to create
- **step**: The number of values to skip before taking the next value

### Supported Simple Types

Sailfish variables can be any type that is compatible with the base **Attribute** class:

```csharp
[SailfishVariable(10, 100, 1000)]           // integers
[SailfishVariable(0.24, 1.6)]               // doubles
[SailfishVariable("ScenarioA", "ScenarioB")] // strings
[SailfishVariable(MyEnum.First, MyEnum.Second)] // enums
```

---

## Complex Variables

For complex object instances, configuration objects, or scenarios requiring sophisticated setup, Sailfish provides two modern approaches that offer better type safety and separation of concerns.

### Interface-Based Approach (Recommended)

The interface-based approach provides the cleanest separation between your data contract and variable generation logic:

```csharp
[Sailfish(NumWarmupIterations = 1, SampleSize = 2)]
public class DatabasePerformanceTest
{
    // Traditional simple variable
    [SailfishVariable(10, 50, 100)]
    public int BufferSize { get; set; }

    // Complex variable using interface approach
    public IDatabaseConfiguration DatabaseConfig { get; set; } = null!;

    [SailfishMethod]
    public void TestDatabaseOperations()
    {
        Console.WriteLine($"Testing with buffer: {BufferSize}");
        Console.WriteLine($"Database: {DatabaseConfig.ConnectionString}");
        Console.WriteLine($"Timeout: {DatabaseConfig.TimeoutSeconds}s");

        // Your performance test logic here
        Thread.Sleep(BufferSize);
    }
}

// Define your data contract interface
public interface IDatabaseConfiguration : ISailfishVariables<DatabaseConfiguration, DatabaseConfigurationProvider>
{
    string ConnectionString { get; }
    int TimeoutSeconds { get; }
    bool EnableRetries { get; }
}

// Clean data type - only concerned with data structure
public record DatabaseConfiguration(
    string ConnectionString,
    int TimeoutSeconds,
    bool EnableRetries) : IDatabaseConfiguration
{
    public int CompareTo(object? obj)
    {
        if (obj is not DatabaseConfiguration other) return 1;

        var connectionComparison = string.Compare(ConnectionString, other.ConnectionString, StringComparison.Ordinal);
        if (connectionComparison != 0) return connectionComparison;

        return TimeoutSeconds.CompareTo(other.TimeoutSeconds);
    }
}

// Separate provider handles variable generation
public class DatabaseConfigurationProvider : ISailfishVariablesProvider<DatabaseConfiguration>
{
    public IEnumerable<DatabaseConfiguration> Variables()
    {
        return new[]
        {
            new DatabaseConfiguration("Server=localhost;Database=TestDB_Fast;", 5, false),
            new DatabaseConfiguration("Server=localhost;Database=TestDB_Reliable;", 30, true),
            new DatabaseConfiguration("Server=localhost;Database=TestDB_Balanced;", 15, true)
        };
    }
}
```

### Class-Based Approach (Alternative)

The class-based approach provides a more direct syntax without requiring custom interfaces:

```csharp
[Sailfish(NumWarmupIterations = 1, SampleSize = 2)]
public class NetworkPerformanceTest
{
    // Traditional simple variable
    [SailfishVariable(100, 500, 1000)]
    public int BufferSize { get; set; }

    // Complex variables using class approach - cleaner syntax!
    public SailfishVariables<MyDatabaseConfig, MyDatabaseConfigProvider> DatabaseConfig { get; set; } = new();
    public SailfishVariables<MyNetworkConfig, MyNetworkConfigProvider> NetworkConfig { get; set; } = new();

    [SailfishMethod]
    public void TestNetworkOperations()
    {
        Console.WriteLine($"Testing with buffer: {BufferSize}");

        // Seamless usage with implicit conversion
        Console.WriteLine($"Database: {DatabaseConfig.Value.ConnectionString}");
        Console.WriteLine($"Network: {NetworkConfig.Value.Protocol}://{NetworkConfig.Value.Host}:{NetworkConfig.Value.Port}");

        // Your performance test logic here
        Thread.Sleep(100);
    }
}

// Simple data types - no interface required
public record MyDatabaseConfig(
    string ConnectionString,
    int TimeoutSeconds,
    bool EnableRetries
) : IComparable
{
    public int CompareTo(object? obj)
    {
        if (obj is MyDatabaseConfig other)
        {
            var connComparison = string.Compare(ConnectionString, other.ConnectionString, StringComparison.Ordinal);
            if (connComparison != 0) return connComparison;
            return TimeoutSeconds.CompareTo(other.TimeoutSeconds);
        }
        return 1;
    }
}

public record MyNetworkConfig(
    string Protocol,
    string Host,
    int Port,
    int MaxConnections
) : IComparable
{
    public int CompareTo(object? obj)
    {
        if (obj is MyNetworkConfig other)
        {
            var protocolComparison = string.Compare(Protocol, other.Protocol, StringComparison.Ordinal);
            if (protocolComparison != 0) return protocolComparison;
            return Port.CompareTo(other.Port);
        }
        return 1;
    }
}

// Providers for variable generation
public class MyDatabaseConfigProvider : ISailfishVariablesProvider<MyDatabaseConfig>
{
    public IEnumerable<MyDatabaseConfig> Variables()
    {
        return new[]
        {
            new MyDatabaseConfig("Server=localhost;Database=TestDB_Fast;", 1, false),
            new MyDatabaseConfig("Server=localhost;Database=TestDB_Reliable;", 1, true),
            new MyDatabaseConfig("Server=localhost;Database=TestDB_Balanced;", 1, true)
        };
    }
}

public class MyNetworkConfigProvider : ISailfishVariablesProvider<MyNetworkConfig>
{
    public IEnumerable<MyNetworkConfig> Variables()
    {
        return new[]
        {
            new MyNetworkConfig("http", "localhost", 8080, 10),
            new MyNetworkConfig("https", "api.example.com", 443, 50),
            new MyNetworkConfig("tcp", "cache.internal", 6379, 100)
        };
    }
}
```

### When to Use Which Approach

**Use Interface-Based Approach when:**
- You want explicit contracts for your data types
- You need to expose specific properties in your test class
- You prefer strong typing with interface contracts
- You want maximum flexibility in data type design

**Use Class-Based Approach when:**
- You want the simplest possible syntax
- You don't need custom interfaces
- You prefer direct property access via `.Value`
- You want minimal boilerplate code

### Benefits of Complex Variables

Both approaches provide significant advantages over simple attribute-based variables:

- **Type Safety**: Full IntelliSense support and compile-time checking
- **Separation of Concerns**: Data types are separate from variable generation logic
- **Reusability**: Providers can be reused across multiple test classes
- **Maintainability**: Changes to variable sets only require updating the provider
- **Flexibility**: Support for nested objects, complex initialization, and sophisticated scenarios
- **Testability**: Providers can be unit tested independently

---

## Mixed Usage Examples

You can combine all variable approaches in a single test class:

```csharp
[Sailfish(NumWarmupIterations = 1, SampleSize = 2)]
public class MixedVariableApproachesExample
{
    // Traditional attribute-based
    [SailfishVariable(10, 20, 30)]
    public int SimpleValue { get; set; }

    // Interface-based approach
    public IDatabaseConfiguration InterfaceBasedConfig { get; set; } = null!;

    // Class-based approach
    public SailfishVariables<MyNetworkConfig, MyNetworkConfigProvider> ClassBasedConfig { get; set; } = new();

    [SailfishMethod]
    public void TestMixedApproaches()
    {
        Console.WriteLine($"Simple: {SimpleValue}");
        Console.WriteLine($"Interface-based: {InterfaceBasedConfig.ConnectionString}");
        Console.WriteLine($"Class-based: {ClassBasedConfig.Value.Protocol}");
    }
}
```

---

## Backward Compatibility

Sailfish maintains full backward compatibility with existing variable patterns:

### Legacy ISailfishComplexVariableProvider (Still Supported)

```csharp
// Legacy pattern - still works but not recommended for new code
public interface ILegacyConfig : ISailfishComplexVariableProvider<LegacyConfig>
{
    string Value { get; }
}

public record LegacyConfig(string Value) : ILegacyConfig
{
    public static IEnumerable<LegacyConfig> GetVariableInstances()
    {
        return new[] { new LegacyConfig("Legacy1"), new LegacyConfig("Legacy2") };
    }

    public int CompareTo(object? obj) => obj is LegacyConfig other ? string.Compare(Value, other.Value, StringComparison.Ordinal) : 1;
}
```

### Migration Guide

**From Legacy ISailfishComplexVariableProvider to New Patterns:**

1. **Extract Provider Logic**: Move `GetVariableInstances()` to a separate provider class
2. **Choose Approach**: Decide between interface-based or class-based approach
3. **Update Interface**: Change from `ISailfishComplexVariableProvider<T>` to `ISailfishVariables<T, TProvider>` or use `SailfishVariables<T, TProvider>`
4. **Update Data Type**: Remove static method, keep `IComparable` implementation

---

## Complexity Estimation (ScaleFish)

When applying a variable attribute, you may choose to specify that variable for ScaleFish complexity estimation and modeling. To do so set the first optional parameter to true:

```csharp
[SailfishVariable(scalefish: true, 10, 100, 1000)]
```

**NOTE**: When using ScaleFish, variables must be of type `int`. Complex variables are not currently supported for complexity estimation.