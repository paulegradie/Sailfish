using Sailfish.Attributes;
using Tests.E2E.TestSuite.Utils;

namespace Tests.E2E.TestSuite.Discoverable;

[Sailfish(SampleSize = 1, Disabled = Constants.Disabled)]
public class TestWithStringVariable
{
    private const string ScenarioA = "ScenarioA";
    private const string ScenarioB = "ScenarioB";
    private readonly Configuration configuration;
    private readonly Dictionary<string, ScenarioData> scenarioMap = new();
    private IClient client = null!;

    public TestWithStringVariable(Configuration configuration)
    {
        this.configuration = configuration;
    }

    [SailfishVariable(ScenarioA, ScenarioB)]
    public string? Scenario { get; set; }

    [SailfishGlobalSetup]
    public void Setup()
    {
        scenarioMap.Add(ScenarioA, configuration.Get(ScenarioA));
        scenarioMap.Add(ScenarioB, configuration.Get(ScenarioB));
    }

    [SailfishMethodSetup]
    public void MethodSetup()
    {
        client = ClientFactory.CreateClient(scenarioMap[Scenario!].Url);
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken ct)
    {
        await client.Get(ct);
    }
}