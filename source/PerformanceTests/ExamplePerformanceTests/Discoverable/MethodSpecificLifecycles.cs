using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(NumIterations = 3, NumWarmupIterations = 2)]
public class MethodSpecificLifecycles
{
    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
    }

    [SailfishMethodSetup(nameof(TestOne))]
    public void ExecutionMethodSetup()
    {
    }

    [SailfishIterationSetup(nameof(TestOne), nameof(TestTwo))]
    public void IterationSetup()
    {
    }

    [SailfishMethod]
    public async Task TestOne(CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
    }

    [SailfishMethod]
    public async Task TestTwo(CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
    }

    [SailfishIterationTeardown(nameof(TestTwo))]
    public void IterationTeardown()
    {
    }

    [SailfishMethodTeardown]
    public void ExecutionMethodTeardown()
    {
    }

    [SailfishGlobalTeardown]
    public async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        await Task.Yield();
    }
}