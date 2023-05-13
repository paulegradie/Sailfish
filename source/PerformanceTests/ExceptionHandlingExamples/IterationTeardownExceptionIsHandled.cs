using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExceptionHandlingExamples;

[Sailfish(NumIterations = 1, NumWarmupIterations = 1, Disabled = false)]
public class IterationTeardownExceptionIsHandled
{
    [SailfishMethod]
    public async Task LifeCycleExceptionTests(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }

    [SailfishIterationTeardown]
    public async Task IterationTeardown(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        throw new Exception("Iteration Teardown Exception");
    }
}