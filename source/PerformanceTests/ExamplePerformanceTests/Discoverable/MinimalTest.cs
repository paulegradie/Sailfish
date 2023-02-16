using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(Disabled = false)]
public class MinimalTest
{
    [SailfishMethod]
    public void Minimal()
    {
    }
}