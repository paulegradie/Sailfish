using System.Threading;
using System.Threading.Tasks;
using PerformanceTests.DemoUtils;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(NumIterations = 3, NumWarmupIterations = 2, Disabled = false)]
public class PerfTestWithAltRego
{
    private readonly ExampleDependencyForAltRego dep;

    public PerfTestWithAltRego(ExampleDependencyForAltRego dep)
    {
        this.dep = dep;
    }

    [SailfishVariable(1, 2, 3)] public int VariableA { get; set; }

    [SailfishMethod]
    public async Task TestA(CancellationToken cancellationToken)
    {
        await Task.Delay(1_000, cancellationToken);
    }

    [SailfishMethod]
    public async Task TestB(CancellationToken cancellationToken)
    {
        await Task.Delay(1_000, cancellationToken);
    }
}