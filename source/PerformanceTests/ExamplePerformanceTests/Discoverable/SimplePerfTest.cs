using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(NumIterations = 2, NumWarmupIterations = 0, Disabled = false)]
public class SimplePerfTest
{
    [SailfishGlobalSetup]
    public async Task GlobalSetup(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    [SailfishMethod]
    public async Task TestA(CancellationToken cancellationToken)
    {
        await Task.Delay(200, cancellationToken);
    }

    [SailfishMethod]
    public async Task TestB(CancellationToken cancellationToken)
    {
        await Task.Delay(100, cancellationToken);
    }

    [SailfishGlobalTeardown]
    public async Task GlobalTearDown(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}