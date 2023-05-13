using Sailfish.Attributes;

namespace Tests.E2E.ExceptionHandling.Tests;

[Sailfish(NumIterations = 1, NumWarmupIterations = 1, Disabled = false)]
public class GlobalTeardownExceptionIsHandled
{
    [SailfishMethod]
    public async Task LifeCycleExceptionTests(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }

    [SailfishGlobalTeardown]
    public async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        throw new Exception("Global Teardown Exception");
    }
}