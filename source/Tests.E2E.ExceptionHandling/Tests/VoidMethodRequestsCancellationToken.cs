using Sailfish.Attributes;

namespace Tests.E2E.ExceptionHandling.Tests;

[Sailfish(NumIterations = 1, NumWarmupIterations = 1, Disabled = false)]
public class VoidMethodRequestsCancellationToken
{
    [SailfishMethod]
    public void MainMethod(CancellationToken cancellationToken)
    {
        Thread.Sleep(10);
    }
}