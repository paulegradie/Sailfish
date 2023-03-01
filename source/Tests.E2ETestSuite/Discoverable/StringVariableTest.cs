using Sailfish.Attributes;
using Tests.E2ETestSuite.Utils;

namespace Tests.E2ETestSuite.Discoverable;

[Sailfish(NumIterations = 3, Disabled = false)]
public class TestWithStringVariable
{
    private IClient client = null!;
    private readonly Configuration configuration;
    private readonly Dictionary<string, ScenarioData> scenarioMap = new();

    public TestWithStringVariable(Configuration configuration)
    {
        this.configuration = configuration;
    }

    private const string ScenarioA = "ScenarioA";
    private const string ScenarioB = "ScenarioB";

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
        await Task.Delay(100, ct);
        await client.Get(ct);
    }
}
