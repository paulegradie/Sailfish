using System.Threading;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.TestNameSpace;

[Sailfish]
public class MinimalTestExample
{
    [SailfishMethod(Order = 3)]
    public void Minimal()
    {
        Thread.Sleep(50);
    }
}