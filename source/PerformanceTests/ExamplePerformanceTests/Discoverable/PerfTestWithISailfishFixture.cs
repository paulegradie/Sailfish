using System.Threading;
using System.Threading.Tasks;
using PerformanceTests.DemoUtils;
using Sailfish.AdapterUtils;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(NumIterations = 3, NumWarmupIterations = 2, Disabled = true)]
public class PerfTestWithISailfishFixture : ISailfishFixture<SailfishDependencies>
{
    private readonly SailfishDependencies sailfishDependencies;

    [SailfishVariable(1, 2, 3)] public int VariableA { get; set; }

    [SailfishMethod]
    public async Task TestA(CancellationToken cancellationToken)
    {
        var testDependency = sailfishDependencies.ResolveType<ExampleDep>();
        await Task.Delay(1_000, cancellationToken);
    }

    [SailfishMethod]
    public async Task TestB(CancellationToken cancellationToken)
    {
        var testDependency = sailfishDependencies.ResolveType<ExampleDep>();
        await Task.Delay(1_000, cancellationToken);
    }

    public PerfTestWithISailfishFixture(SailfishDependencies sailfishDependencies)
    {
        this.sailfishDependencies = sailfishDependencies;
    }
}