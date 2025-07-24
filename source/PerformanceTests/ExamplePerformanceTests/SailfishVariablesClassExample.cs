using System;
using System.Collections.Generic;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;

namespace PerformanceTests.ExamplePerformanceTests;

/// <summary>
/// Example demonstrating the new SailfishVariables&lt;T, TProvider&gt; class approach.
/// This provides a cleaner API compared to the interface-based ISailfishVariables pattern
/// by eliminating the need for custom interfaces.
/// </summary>
[Sailfish(NumWarmupIterations = 1, SampleSize = 2, DisableOverheadEstimation = true)]
public class SailfishVariablesClassExample
{
    // Traditional attribute-based variable for comparison
    [SailfishVariable(100, 500, 1000)]
    public int BufferSize { get; set; }

    // NEW: Direct SailfishVariables<T, TProvider> class usage - much cleaner!
    public SailfishVariables<MyDatabaseConfig, MyDatabaseConfigProvider> DatabaseConfig { get; set; } = new();

    public SailfishVariables<MyNetworkConfig, MyNetworkConfigProvider> NetworkConfig { get; set; } = new();

    [SailfishMethod]
    public void TestDatabaseOperations()
    {
        Console.WriteLine($"Testing with buffer: {BufferSize}");
        
        // Implicit conversion to the underlying type - seamless usage!
        Console.WriteLine($"Database: {DatabaseConfig.Value.ConnectionString} (timeout: {DatabaseConfig.Value.TimeoutSeconds}s)");
        Console.WriteLine($"Network: {NetworkConfig.Value.Protocol}://{NetworkConfig.Value.Host}:{NetworkConfig.Value.Port}");

        // Simulate database work
        System.Threading.Thread.Sleep(10);
    }
}

/// <summary>
/// Example showing both approaches working together in the same test class
/// </summary>
[Sailfish(NumWarmupIterations = 1, SampleSize = 2)]
public class MixedVariableApproachesExample
{
    // Traditional attribute-based
    [SailfishVariable(10, 20, 30)]
    public int SimpleValue { get; set; }

    // Interface-based approach (existing)
    public IMyDatabaseConfig InterfaceBasedConfig { get; set; } = null!;

    // Class-based approach (new)
    public SailfishVariables<MyNetworkConfig, MyNetworkConfigProvider> ClassBasedConfig { get; set; } = new();

    [SailfishMethod]
    public void TestMixedApproaches()
    {
        Console.WriteLine($"Simple: {SimpleValue}");
        Console.WriteLine($"Interface-based: {InterfaceBasedConfig.ConnectionString}");
        Console.WriteLine($"Class-based: {ClassBasedConfig.Value.Protocol}");
    }
}

// Data types for the new class-based approach
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

            var timeoutComparison = TimeoutSeconds.CompareTo(other.TimeoutSeconds);
            if (timeoutComparison != 0) return timeoutComparison;

            return EnableRetries.CompareTo(other.EnableRetries);
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

            var hostComparison = string.Compare(Host, other.Host, StringComparison.Ordinal);
            if (hostComparison != 0) return hostComparison;

            var portComparison = Port.CompareTo(other.Port);
            if (portComparison != 0) return portComparison;

            return MaxConnections.CompareTo(other.MaxConnections);
        }
        return 1;
    }
}

// Providers for the new class-based approach
public class MyDatabaseConfigProvider : ISailfishVariablesProvider<MyDatabaseConfig>
{
    public IEnumerable<MyDatabaseConfig> Variables()
    {
        return new[]
        {
            new MyDatabaseConfig(
                "Server=localhost;Database=TestDB_Fast;",
                1,
                false
            ),
            new MyDatabaseConfig(
                "Server=localhost;Database=TestDB_Reliable;",
                1,
                true
            ),
            new MyDatabaseConfig(
                "Server=localhost;Database=TestDB_Balanced;",
                1,
                true
            )
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

// Interface-based approach (existing) - for comparison
public interface IMyDatabaseConfig : ISailfishVariables<MyDatabaseConfig, MyDatabaseConfigProvider>
{
    string ConnectionString { get; }
    int TimeoutSeconds { get; }
    bool EnableRetries { get; }
}
