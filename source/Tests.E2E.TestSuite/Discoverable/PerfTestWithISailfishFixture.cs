using Sailfish.Attributes;
using Sailfish.Registration;
using Tests.E2ETestSuite.Utils;

namespace Tests.E2ETestSuite.Discoverable;

[Sailfish(NumIterations = 1, NumWarmupIterations = 1, Disabled = Constants.Disabled)]
public class PerfTestWithISailfishFixture : ISailfishFixture<SailfishDependencies>
{
    private readonly SailfishDependencies sailfishDependencies;

    [SailfishVariable(1, 2, 3)] public int VariableA { get; set; }

    [SailfishMethod]
    public async Task TestA(CancellationToken cancellationToken)
    {
        var testDependency = sailfishDependencies.ResolveType<ExampleDep>();
        await Task.Delay(10, cancellationToken);
    }

    [SailfishMethod]
    public async Task TestB(CancellationToken cancellationToken)
    {
        var testDependency = sailfishDependencies.ResolveType<ExampleDep>();
        await Task.Delay(14, cancellationToken);
    }

    public PerfTestWithISailfishFixture(SailfishDependencies sailfishDependencies)
    {
        this.sailfishDependencies = sailfishDependencies;
    }
}