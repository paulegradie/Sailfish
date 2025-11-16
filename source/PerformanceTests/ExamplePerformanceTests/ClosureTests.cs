using System;
using System.Threading.Tasks;
using Sailfish.Attributes;
using System.Collections.Generic;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToMarkdown]
[Sailfish(SampleSize = 3, Disabled = false, DisableOverheadEstimation = true)]
public class ScenarioSystem
{
    private readonly Random _random = new();

    [SailfishVariable(0, 1, 2)] 
    public int Index { get; set; }

    private int Count { get; set; }

    public Scenario Data { get; set; } = null!;

    [SailfishMethodSetup]
    public void Setup()
    {
        Count = 0;
    }

    [SailfishIterationSetup]
    public void EachIterationDo()
    {
        Data = MyDataContainer.GetDataForScenarioIndex(Index);
    }

    [SailfishMethod]
    public async Task Scenario() // token is injected when requested
    {
        await Task.Yield();
        Console.WriteLine($"{Data.Name}");
    }
}

public class MyDataContainer
{
    private static List<Scenario> ScenarioList { get; set; } = [];

    static MyDataContainer()
    {
        ScenarioList.Add(new Scenario("MaxConn=12"));
        ScenarioList.Add(new Scenario("B"));
        ScenarioList.Add(new Scenario("C"));
    }

    public static Scenario GetDataForScenarioIndex(int scenarioIndex)
    {
        return ScenarioList[scenarioIndex];
    }
};

public record Scenario
{
    public Scenario(string Name)
    {
        this.Name = Name;
    }

    public string Name { get; init; }

    public void Deconstruct(out string Name)
    {
        Name = this.Name;
    }
}