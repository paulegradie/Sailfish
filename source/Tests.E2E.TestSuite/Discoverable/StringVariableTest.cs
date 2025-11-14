using Sailfish.Attributes;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tests.E2E.TestSuite.Utils;

namespace Tests.E2E.TestSuite.Discoverable;

[Sailfish(SampleSize = 1, Disabled = Constants.Disabled)]
public class TestWithStringVariable
{
    private const string ScenarioA = "ScenarioA";
    private const string ScenarioB = "ScenarioB";
    private readonly Configuration _configuration;
    private readonly Dictionary<string, ScenarioData> _scenarioMap = new();
    private IClient _client = null!;

    public TestWithStringVariable(Configuration configuration)
    {
        this._configuration = configuration;
    }

    [SailfishVariable(ScenarioA, ScenarioB)]
    public string? Scenario { get; set; }

    [SailfishGlobalSetup]
    public void Setup()
    {
        _scenarioMap.Add(ScenarioA, _configuration.Get(ScenarioA));
        _scenarioMap.Add(ScenarioB, _configuration.Get(ScenarioB));
    }

    [SailfishMethodSetup]
    public void MethodSetup()
    {
        _client = ClientFactory.CreateClient(_scenarioMap[Scenario!].Url);
    }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken ct)
    {
        await _client.Get(ct);
    }
}