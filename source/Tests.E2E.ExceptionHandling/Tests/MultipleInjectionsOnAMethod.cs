using Sailfish.Attributes;
using Sailfish.Logging;

namespace Tests.E2E.ExceptionHandling.Tests;

[Sailfish(SampleSize = 1, NumWarmupIterations = 1, Disabled = false)]
public class MultipleInjectionsOnAsyncMethod
{
    [SailfishMethod]
    public async Task MainMethod(ILogger logger, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }
}