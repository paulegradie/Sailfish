using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;
using System;

namespace PerformanceTests.ExamplePerformanceTests;

/// <summary>
/// Demonstrates scenario-based performance testing with complex objects and multiple variables.
/// This example shows how different connection types (FTP, HTTPS, Database) can have
/// different performance characteristics that can be measured and compared.
/// </summary>
[WriteToMarkdown]
[WriteToCsv]
[Sailfish(SampleSize = 5, Disabled = false, DisableOverheadEstimation = true)]
public class ScenariosExample
{
    private const string ScenarioA = "ScenarioA";
    private const string ScenarioB = "ScenarioB";
    private const string ScenarioC = "ScenarioC";
    private Dictionary<string, MyScenario> _scenarioMap = null!;

    /// <summary>
    /// Controls the complexity of operations - "wow" triggers multiple operations per test
    /// </summary>
    [SailfishVariable("wow", "ok")]
    public string N { get; set; } = null!;

    /// <summary>
    /// Defines which connection scenario to test - each has different performance characteristics
    /// </summary>
    [SailfishVariable(ScenarioA, ScenarioB, ScenarioC)]
    public string Scenario { get; set; } = null!;

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        _scenarioMap = new Dictionary<string, MyScenario>
        {
            { ScenarioA, new MyScenario("ftp://test.example.com", 21, new InnerScenario("FTP_Transfer")) },
            { ScenarioB, new MyScenario("https://api.example.com", 443, new InnerScenario("HTTPS_API")) },
            { ScenarioC, new MyScenario("tcp://db.example.com", 5432, new InnerScenario("Database_Query")) }
        };
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken cancellationToken) // token is injected when requested
    {
        var scenario = _scenarioMap[Scenario];
        Console.WriteLine($"Testing scenario: {scenario.InnerScenario.Name} on {scenario.ConnStr}:{scenario.Port}");

        // Simulate different operations based on scenario type with varying complexity based on N variable
        var operationCount = N == "wow" ? 3 : 1; // Use the N variable to control operation complexity

        for (var i = 0; i < operationCount; i++)
        {
            await SimulateScenarioOperation(scenario, cancellationToken);
        }
    }

    private async Task SimulateScenarioOperation(MyScenario scenario, CancellationToken cancellationToken)
    {
        // Different scenarios have different performance characteristics
        var delay = scenario.InnerScenario.Name switch
        {
            "FTP_Transfer" => 150,      // FTP operations are typically slower
            "HTTPS_API" => 75,          // API calls are moderate speed
            "Database_Query" => 50,     // Database queries are typically fastest
            _ => 100
        };

        // Simulate connection establishment (varies by port/protocol)
        var connectionDelay = scenario.Port switch
        {
            21 => 25,   // FTP connection overhead
            443 => 15,  // HTTPS handshake
            5432 => 10, // Database connection
            _ => 20
        };

        await Task.Delay(connectionDelay, cancellationToken);
        await Task.Delay(delay, cancellationToken);

        Console.WriteLine($"Completed operation for {scenario.InnerScenario.Name}");
    }

    record MyScenario(string ConnStr, int Port, InnerScenario InnerScenario);

    record InnerScenario(string Name);
}