using System.Threading;
using System.Threading.Tasks;
using PerformanceTests.DemoUtils;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(NumIterations = 1, NumWarmupIterations = 0, Disabled = TestConstants.Disabled)]
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
        await Task.Delay(500, cancellationToken);
    }

    [SailfishMethod]
    public async Task TestB(CancellationToken cancellationToken)
    {
        await Task.Delay(300, cancellationToken);
    }

    [SailfishGlobalTeardown]
    public async Task GlobalTearDown(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}