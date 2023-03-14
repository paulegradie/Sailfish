using Sailfish.Attributes;
using Tests.E2ETestSuite.Discoverable;

namespace Tests.E2E.ExceptionHandling.Tests;

[Sailfish]
public class ADuplicateLifeCycle : ATestBase
{
    [SailfishIterationSetup]
    public async Task TheIterationSetup(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }

    [SailfishMethod]
    public async Task TheSingularMethod(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }
}