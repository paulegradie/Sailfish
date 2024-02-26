using Sailfish.Attributes;
using System.Threading;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(SampleSize = 3, DisableOverheadEstimation = true, NumWarmupIterations = 0)]
public class MinimalTestExample
{
    [SailfishMethod(Order = 3)]
    public void Minimal()
    {
        Thread.Sleep(50);
    }
}