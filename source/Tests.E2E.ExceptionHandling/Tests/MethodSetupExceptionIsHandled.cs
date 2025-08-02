using Sailfish.Attributes;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.E2E.ExceptionHandling.Tests;

[Sailfish(SampleSize = 1, NumWarmupIterations = 1, Disabled = false)]
public class MethodSetupExceptionIsHandled
{
    [SailfishMethodSetup]
    public void MethodSetup()
    {
        throw new TestException("Method Setup Exception");
    }

    [SailfishMethod]
    public async Task LifeCycleExceptionTests(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }
}