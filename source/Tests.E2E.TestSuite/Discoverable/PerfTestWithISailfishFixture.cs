using Sailfish.Attributes;
using Sailfish.Registration;
using System.Threading;
using System.Threading.Tasks;
using Tests.E2E.TestSuite.Utils;

namespace Tests.E2E.TestSuite.Discoverable;

[Sailfish(SampleSize = 1, NumWarmupIterations = 1, Disabled = Constants.Disabled)]
public class PerfTestWithISailfishFixture : ISailfishFixture<SailfishDependencies>
{
    private readonly SailfishDependencies _sailfishDependencies;

    public PerfTestWithISailfishFixture(SailfishDependencies sailfishDependencies)
    {
        this._sailfishDependencies = sailfishDependencies;
    }

    [SailfishVariable(1, 2, 3)]
    public int VariableA { get; set; }

    [SailfishMethod]
    public async Task TestA(CancellationToken cancellationToken)
    {
        _sailfishDependencies.ResolveType<ExampleDep>();
        await Task.Delay(10, cancellationToken);
    }

    [SailfishMethod]
    public async Task TestB(CancellationToken cancellationToken)
    {
        _sailfishDependencies.ResolveType<ExampleDep>();
        await Task.Delay(14, cancellationToken);
    }
}