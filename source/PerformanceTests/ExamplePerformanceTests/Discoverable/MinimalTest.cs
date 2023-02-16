using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(Disabled = true)]
public class MinimalTest
{
    [SailfishMethod]
    public void Minimal()
    {
    }
}