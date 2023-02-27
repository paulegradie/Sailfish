using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(Disabled = false)]
public class MinimalTest
{
    [SailfishMethod]
    public void Minimal()
    {
    }
}