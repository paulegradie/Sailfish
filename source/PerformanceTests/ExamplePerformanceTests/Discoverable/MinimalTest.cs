using System.Threading;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(Disabled = false)]
public class MinimalTest
{
    [SailfishIterationSetup]
    public void Setup()
    {
        Thread.Sleep(10);
    }
    
    [SailfishMethod]
    public void Minimal()
    {
        Thread.Sleep(100);
    }
}