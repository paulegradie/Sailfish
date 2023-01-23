using System.Threading;
using System.Threading.Tasks;
using Sailfish.AdapterUtils;
using Sailfish.Attributes;
using Tests.UsingTheIDE.DemoUtils;

namespace Tests.UsingTheIDE;

[Sailfish(NumIterations = 3, NumWarmupIterations = 2)]
public class SimplePerfTest : ISailfishFixture<SailfishDependencies>
{
    private readonly SailfishDependencies sailfishDependencies;

    [SailfishVariable(1, 2, 3)] public int VariableA { get; set; }

    [SailfishMethod]
    public async Task Go(CancellationToken cancellationToken)
    {
        var testDependency = sailfishDependencies.ResolveType<ExampleDep>();
        await Task.Delay(1_000, cancellationToken);
    }

    [SailfishMethod]
    public async Task GoAgain(CancellationToken cancellationToken)
    {
        var testDependency = sailfishDependencies.ResolveType<ExampleDep>();
        await Task.Delay(1_000, cancellationToken);
    }

    public SimplePerfTest(SailfishDependencies sailfishDependencies)
    {
        this.sailfishDependencies = sailfishDependencies;
    }
}