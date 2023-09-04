using System.Threading;
using System.Threading.Tasks;
using PerformanceTests.DemoUtils;
using Sailfish.Attributes;
using Sailfish.Registration;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(NumIterations = 3, NumWarmupIterations = 2, Disabled = false)]
public class ISailfishFixtureExample : ISailfishFixture<SailfishDependencies>
{
    private readonly SailfishDependencies sailfishDependencies;

    [SailfishVariable(1, 2, 3)] public int VariableA { get; set; }

    [SailfishMethod]
    public async Task TestA(CancellationToken cancellationToken)
    {
        var testDependency = sailfishDependencies.ResolveType<ExampleDep>();
        await Task.Delay(100, cancellationToken);
    }

    [SailfishMethod]
    public async Task TestB(CancellationToken cancellationToken)
    {
        var testDependency = sailfishDependencies.ResolveType<ExampleDep>();
        await Task.Delay(100, cancellationToken);
    }

    public ISailfishFixtureExample(SailfishDependencies sailfishDependencies)
    {
        this.sailfishDependencies = sailfishDependencies;
    }
}