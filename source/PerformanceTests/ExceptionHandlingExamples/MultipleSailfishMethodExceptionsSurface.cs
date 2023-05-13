using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExceptionHandlingExamples;

[Sailfish(NumIterations = 1, NumWarmupIterations = 1, Disabled = false)]
public class MultipleLifecycleMethodExceptionsSurfaceWhenThereAreMultipleMethods
{
    [SailfishMethod]
    public async Task FirstMethod(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }

    [SailfishMethod]
    public async Task SecondMethod(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }

    [SailfishMethodTeardown]
    public void MethodTeardown(CancellationToken cancellationToken)
    {
        throw new Exception("Method Teardown Exception");
    }

    [SailfishGlobalTeardown]
    public void GlobalTeardown(CancellationToken cancellationToken)
    {
        throw new Exception("Global Teardown Exception");
    }
}