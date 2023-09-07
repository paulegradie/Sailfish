using Sailfish.Attributes;

namespace Tests.E2E.ExceptionHandling.Tests;

[Sailfish(NumSamples = 1, NumWarmupIterations = 1, Disabled = false)]
public class OnlyTheSailfishMethodThrows
{
    [SailfishMethod]
    public async Task SailfishMethodException(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        throw new Exception("Sailfish Method Exception");
    }

    [SailfishIterationSetup]
    public async Task IterationSetup(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }

    [SailfishMethodTeardown]
    public async Task MethodTeardown(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }
}