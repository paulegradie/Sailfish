using System.Threading;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.NonDiscoverable;

[Sailfish(Disabled = false)]
public class DisabledTestIsNotDiscoverable
{
    [SailfishMethod]
    public void MethodShouldNotHavePlayButton()
    {
        Thread.Sleep(1000);
    }
}