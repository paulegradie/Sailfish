using Sailfish.Attributes;

namespace Tests.E2E.ExceptionHandling.Tests;

[Sailfish(NumIterations = 1, NumWarmupIterations = 1, Disabled = false)]
public class IterationSetupExceptionComesFirst
{
    [SailfishIterationSetup]
    public async Task IterationSetup(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new Exception("Iteration Setup Exception");
    }

    [SailfishMethod]
    public async Task LifeCycleExceptionTests(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }

    [SailfishMethod]
    public async Task SailfishMethodException(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        throw new Exception("Sailfish Method Exception");
    }

    [SailfishMethodTeardown]
    public async Task MethodTeardown(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        throw new Exception("Method Teardown Exception");
    }
}