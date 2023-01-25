using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;
using Sailfish.Registration;

namespace PerformanceTests.ExamplePerformanceTests;

public class ExampleDependencyForAltRego : ISailfishDependency
{
}

[Sailfish(NumIterations = 3, NumWarmupIterations = 2)]
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