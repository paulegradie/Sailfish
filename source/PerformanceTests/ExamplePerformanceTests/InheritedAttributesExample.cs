using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[Sailfish(NumSamples = 3, NumWarmupIterations = 1, Disabled = false)]
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
        Console.WriteLine($"Global Setup was run from {nameof(AttributesInheritedBase)}");
    }

    [SailfishMethod]
    public void MethodOnBaseShouldNotExecute()
    {
        Thread.Sleep(10);
        Console.WriteLine("Base test method won't be run");
    }
}