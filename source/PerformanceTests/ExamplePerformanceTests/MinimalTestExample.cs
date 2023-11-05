using System.Threading;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(SampleSize = 5, DisableOverheadEstimation = true, NumWarmupIterations = 0)]
public class MinimalTestExample
{
    [SailfishIterationSetup]
    public void Setup()
    {
        Thread.Sleep(10);
    }

    [SailfishMethod]
    public void Minimal()
    {
        Thread.Sleep(50);
    }
    
    [SailfishMethod]
    public void OtherMinimal()
    {
        Thread.Sleep(50);
    }
    
}