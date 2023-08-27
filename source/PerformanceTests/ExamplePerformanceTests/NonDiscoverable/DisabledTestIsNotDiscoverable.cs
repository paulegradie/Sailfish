using System.Threading;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.NonDiscoverable;

[Sailfish(Disabled = true)]
public class DisabledTestIsNotDiscoverable
{
    [SailfishMethod]
    public void MethodShouldNotHavePlayButton()
    {
        Thread.Sleep(300);
    }
}