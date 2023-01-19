using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace Tests.UsingTheIDE;

[Sailfish(NumIterations = 3, NumWarmupIterations = 2)]
public class SimplePerfTest : SailfishBase
{
    private readonly ExampleDep exampleDep;

    [SailfishVariable(1, 2, 3)] public int VariableA { get; set; }

    [SailfishMethod] 
    public async Task Go(CancellationToken cancellationToken)
    {
        await Task.Delay(1_000, cancellationToken);
    }

    public SimplePerfTest(ExampleDep exampleDep, SailfishDependencies sailfishDependencies) : base(sailfishDependencies)
    {
        this.exampleDep = exampleDep;
    }
}