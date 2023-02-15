using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(Disabled = true)]
public class MinimalTest
{
    [SailfishMethod]
    public void Minimal()
    {
    }
}