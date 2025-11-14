using Sailfish.Attributes;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.E2E.TestSuite.Discoverable;

[Sailfish(Disabled = Constants.Disabled)]
public class ScenariosExample
{
    private const string ScenarioA = "ScenarioA";
    private const string ScenarioB = "ScenarioB";
    private Dictionary<string, string> _scenarioMap = null!;

    [SailfishVariable("wow", "ok")]
    public string N { get; set; } = null!;

    [SailfishVariable(ScenarioA, ScenarioB)]
    public string? Scenario { get; set; }

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        _scenarioMap = new Dictionary<string, string> { { ScenarioA, "OK" }, { ScenarioB, "wow" } };
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken cancellationToken) // token is injected when requested
    {
        Console.WriteLine(_scenarioMap[Scenario!]);
        await Task.Delay(18, cancellationToken);
    }
}