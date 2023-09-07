using Sailfish.Attributes;
using Tests.E2ETestSuite.Utils;

namespace Tests.E2ETestSuite.Discoverable;

[Sailfish(SampleSize = 1, NumWarmupIterations = 1, Disabled = Constants.Disabled)]
public class PerfTestWithAltRego
{
    private readonly ExampleDependencyForAltRego dep;

    public PerfTestWithAltRego(ExampleDependencyForAltRego dep)
    {
        this.dep = dep;
    }

    [SailfishVariable(1, 2)] public int VariableA { get; set; }

    [SailfishMethod]
    public async Task TestA(CancellationToken cancellationToken)
    {
        await Task.Delay(15, cancellationToken);
    }

    [SailfishMethod]
    public async Task TestB(CancellationToken cancellationToken)
    {
        await Task.Delay(12, cancellationToken);
    }
}