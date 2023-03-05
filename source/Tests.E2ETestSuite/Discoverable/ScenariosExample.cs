﻿using Sailfish.Attributes;

namespace Tests.E2ETestSuite.Discoverable;

[Sailfish(Disabled = false)]
public class ScenariosExample
{
    private Dictionary<string, string> scenarioMap = null!;

    [SailfishVariable("wow", "ok")] public string N { get; set; } = null!;

    private const string ScenarioA = "ScenarioA";
    private const string ScenarioB = "ScenarioB";

    [SailfishVariable(ScenarioA, ScenarioB)]
    public string Scenario { get; set; } = null!;

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        scenarioMap = new Dictionary<string, string>
        {
            { ScenarioA, "OK" },
            { ScenarioB, "wow" }
        };
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken cancellationToken) // token is injected when requested
    {
        Console.WriteLine(scenarioMap[Scenario]);
        await Task.Delay(18, cancellationToken);
    }
}