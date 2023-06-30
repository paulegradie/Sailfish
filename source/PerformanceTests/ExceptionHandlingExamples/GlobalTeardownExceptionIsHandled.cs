using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExceptionHandlingExamples;

[Sailfish(NumIterations = 1, NumWarmupIterations = 1, Disabled = false)]
public class GlobalTeardownExceptionIsHandled
{
    [SailfishMethod]
    public async Task LifeCycleExceptionTestsAsync(CancellationToken cancellationToken)
    {
        // do nothing but wait
        await Task.Delay(10, cancellationToken);
    }

    [SailfishGlobalTeardown]
    public async Task GlobalTeardownAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        throw new Exception("Global Teardown Exception");
    }
}