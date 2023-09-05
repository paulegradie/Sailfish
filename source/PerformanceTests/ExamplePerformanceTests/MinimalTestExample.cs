using System.Threading;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(Disabled = false)]
public class MinimalTestExample
{
    [SailfishIterationSetup]
    public void Setup()
    {
        Thread.Sleep(10);
    }

    [SailfishMethod(DisableOverheadEstimation = true)]
    public void Minimal()
    {
        Thread.Sleep(50);
    }
}