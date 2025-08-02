using Sailfish.Attributes;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.E2E.ExceptionHandling.Tests;

[Sailfish(SampleSize = 1, NumWarmupIterations = 1, Disabled = false)]
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
        throw new TestException("Global Teardown Exception");
    }
}