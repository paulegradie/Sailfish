using Sailfish.Attributes;

namespace Tests.E2ETestSuite.Discoverable;

public class UsesBaseLifecycleMethods : ATestBase
{
    protected override async Task GlobalSetup(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }

    protected override async Task MethodSetup(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }

    protected override async Task IterationSetup(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }

    [SailfishMethod]
    public async Task MyTestMethod(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }

    protected override async Task IterationTeardown(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }

    protected override async Task MethodTeardown(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }

    protected override async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }
}