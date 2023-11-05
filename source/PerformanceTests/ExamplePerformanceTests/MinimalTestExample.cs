using System.Threading;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(SampleSize = 1, NumWarmupIterations = 0)]
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