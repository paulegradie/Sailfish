using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[Sailfish(SampleSize = 3, NumWarmupIterations = 1, Disabled = false)]
public class ChildTestClass : AttributesInheritedBase
{
    [SailfishMethod]
    public async Task TestMethodForInheritanceTests(CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
    }
}

public class AttributesInheritedBase
{
    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
    }

    [SailfishMethod]
    public void MethodOnBaseShouldNotExecute()
    {
        Thread.Sleep(10);
    }
}