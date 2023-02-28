using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;
using Sailfish.Registration;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(NumIterations = 3, Disabled = false)]
public class TestWithStringVariable
{
    private IClient client = null!;
    private readonly Configuration configuration = null!;
    private readonly Dictionary<string, ScenarioData> scenarioMap = new();

    public TestWithStringVariable(Configuration configuration)
    {
        this.configuration = configuration;
        scenarioMap.Add(ScenarioA, configuration.Get(ScenarioA));
        scenarioMap.Add(ScenarioB, configuration.Get(ScenarioB));
    }

    private const string ScenarioA = "ScenarioA";
    private const string ScenarioB = "ScenarioB";

    [SailfishVariable(ScenarioA, ScenarioB)]
    public string? Scenario { get; set; }

    // [SailfishGlobalSetup]
    // public void Setup()
    // {
    // }

    [SailfishMethodSetup]
    public void MethodSetup()
    {
        client = ClientFactory.CreateClient(scenarioMap[Scenario].Url);
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken ct)
    {
        await Task.Delay(100, ct);
        await client.Get(ct);
    }
}

internal interface IClient
{
    Task Get(CancellationToken cancellationToken);
}

public class Client : IClient
{
    public async Task Get(CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken);
    }
}

internal static class ClientFactory
{
    public static IClient CreateClient(string url)
    {
        return new Client();
    }
}

public class ScenarioData
{
    public ScenarioData(string key)
    {
        Url = "https://example.com";
    }

    public string Url { get; set; }
}

public class Configuration : ISailfishDependency
{
    public ScenarioData Get(string key)
    {
        return new ScenarioData(key);
    }
}