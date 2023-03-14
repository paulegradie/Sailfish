using Sailfish.Attributes;

namespace Tests.E2ETestSuite.Discoverable;

public class ATestBase
{
    [SailfishGlobalSetup]
    public async Task EnforcedGlobalSetup(CancellationToken cancellationToken)
    {
        await GlobalSetup(cancellationToken);
    }

    [SailfishMethodSetup]
    public async Task EnforcedMethodSetup(CancellationToken cancellationToken)
    {
        await MethodSetup(cancellationToken);
    }

    [SailfishIterationSetup]
    public async Task EnforcedIterationSetup(CancellationToken cancellationToken)
    {
        await IterationSetup(cancellationToken);
    }

    [SailfishIterationTeardown]
    public async Task EnforcedIterationTeardown(CancellationToken cancellationToken)
    {
        await IterationTeardown(cancellationToken);
    }

    [SailfishMethodTeardown]
    public async Task EnforcedMethodTeardown(CancellationToken cancellationToken)
    {
        await MethodTeardown(cancellationToken);
    }

    [SailfishGlobalTeardown]
    public async Task EnforcedGlobalTeardown(CancellationToken cancellationToken)
    {
        await GlobalTeardown(cancellationToken);
    }

    protected virtual Task GlobalSetup(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task MethodSetup(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task IterationSetup(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task IterationTeardown(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task MethodTeardown(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task GlobalTeardown(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}