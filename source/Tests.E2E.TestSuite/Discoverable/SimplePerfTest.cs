using Sailfish.Attributes;

namespace Tests.E2ETestSuite.Discoverable;

[Sailfish(NumSamples = 3, NumWarmupIterations = 0, Disabled = Constants.Disabled)]
public class SimplePerfTest
{
    [SailfishGlobalSetup]
    public async Task GlobalSetup(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    [SailfishVariable(1, 2, 3)] public int TestProp { get; set; }

    [SailfishMethod]
    public async Task TestA(CancellationToken cancellationToken)
    {
        await Task.Delay(40 + TestProp, cancellationToken);
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