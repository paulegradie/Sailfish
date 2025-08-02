using Sailfish.Attributes;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.E2E.ExceptionHandling.Tests;

[Sailfish(SampleSize = 1, NumWarmupIterations = 1, Disabled = false)]
public class GlobalSetupExceptionIsHandled
{
    [SailfishGlobalSetup]
    public async Task GlobalSetup(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        throw new TestException("Global Setup Exception");
    }

    [SailfishMethod]
    public async Task LifeCycleExceptionTests(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }
}