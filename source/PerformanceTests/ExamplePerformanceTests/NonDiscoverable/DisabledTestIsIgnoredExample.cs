using Sailfish.Attributes;
using System.Threading;

namespace PerformanceTests.ExamplePerformanceTests.NonDiscoverable;

[Sailfish(Disabled = true)]
public class DisabledTestIsIgnoredExample
{
    [SailfishMethod]
    public void MethodShouldNotHavePlayButton()
    {
        Thread.Sleep(300);
    }
}