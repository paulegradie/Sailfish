using Sailfish.Attributes;
using Sailfish.Contracts.Public.Variables;
using System;
using System.Collections.Generic;

namespace PerformanceTests.ExamplePerformanceTests;

/// <summary>
/// Example demonstrating the new ISailfishVariables&lt;Type, TypeProvider&gt; pattern.
/// This pattern provides better separation of concerns by separating data types from variable generation logic.
/// </summary>
[Sailfish(NumWarmupIterations = 1, SampleSize = 2)]
public class TypedVariablesExample
{
    // Traditional attribute-based variable for comparison
    [SailfishVariable(100, 500, 1000)]
    public int BufferSize { get; set; }

    // NEW: ISailfishVariables<Type, TypeProvider> pattern
    public IDatabaseConfiguration DatabaseConfig { get; set; } = null!;

    public INetworkConfiguration NetworkConfig { get; set; } = null!;

    [SailfishMethod]
    public void TestDatabaseOperations()
    {
        Console.WriteLine($"Testing with buffer: {BufferSize}");
        Console.WriteLine($"Database: {DatabaseConfig.ConnectionString} (timeout: {DatabaseConfig.TimeoutSeconds}s)");
        Console.WriteLine($"Network: {NetworkConfig.Protocol}://{NetworkConfig.Host}:{NetworkConfig.Port}");
        
        // Simulate database work
        System.Threading.Thread.Sleep(DatabaseConfig.TimeoutSeconds * 10);
    }
}

// Database configuration interface using the new pattern
public interface IDatabaseConfiguration : ISailfishVariables<DatabaseConfiguration, DatabaseConfigurationProvider>
{
    string ConnectionString { get; }
    int TimeoutSeconds { get; }
    bool EnableRetries { get; }
}

// Separate provider for database configurations
public class DatabaseConfigurationProvider : ISailfishVariablesProvider<DatabaseConfiguration>
{
    public IEnumerable<DatabaseConfiguration> Variables()
    {
        return new[]
        {
            new DatabaseConfiguration(
                "Server=localhost;Database=TestDB_Fast;",
                5,
                false
            ),
            new DatabaseConfiguration(
                "Server=localhost;Database=TestDB_Reliable;",
                30,
                true
            ),
            new DatabaseConfiguration(
                "Server=localhost;Database=TestDB_Balanced;",
                15,
                true
            )
        };
    }
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

// Network configuration interface using the new pattern
public interface INetworkConfiguration : ISailfishVariables<NetworkConfiguration, NetworkConfigurationProvider>
{
    string Protocol { get; }
    string Host { get; }
    int Port { get; }
    int MaxConnections { get; }
}

// Separate provider for network configurations
public class NetworkConfigurationProvider : ISailfishVariablesProvider<NetworkConfiguration>
{
    public IEnumerable<NetworkConfiguration> Variables()
    {
        return new[]
        {
            new NetworkConfiguration("http", "localhost", 8080, 10),
            new NetworkConfiguration("https", "api.example.com", 443, 50),
            new NetworkConfiguration("tcp", "cache.internal", 6379, 100)
        };
    }
}

// Clean data type - only concerned with data structure
public record NetworkConfiguration(
    string Protocol,
    string Host,
    int Port,
    int MaxConnections) : INetworkConfiguration
{
    public int CompareTo(object? obj)
    {
        if (obj is not NetworkConfiguration other) return 1;
        
        var protocolComparison = string.Compare(Protocol, other.Protocol, StringComparison.Ordinal);
        if (protocolComparison != 0) return protocolComparison;
        
        var hostComparison = string.Compare(Host, other.Host, StringComparison.Ordinal);
        if (hostComparison != 0) return hostComparison;
        
        return Port.CompareTo(other.Port);
    }
}
