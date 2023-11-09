using System.Threading;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(SampleSize = 10, DisableOverheadEstimation = true, NumWarmupIterations = 0)]
public class MinimalTestExample
{
    [SailfishMethod(Order = 3)]
    public void Minimal() => Thread.Sleep(50);
}