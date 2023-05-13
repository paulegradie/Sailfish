using Sailfish.Attributes;

namespace Tests.E2ETestSuite.Discoverable;

[Sailfish(NumIterations = 1, NumWarmupIterations = 0, Disabled = false)]
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
        await Task.Delay(40, cancellationToken);
    }

    [SailfishMethod]
    public async Task TestB(CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
    }

    [SailfishGlobalTeardown]
    public async Task GlobalTearDown(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}