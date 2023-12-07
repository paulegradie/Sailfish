using Sailfish.Attributes;
using System.Threading;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(SampleSize = 1, DisableOverheadEstimation = true, NumWarmupIterations = 0)]
public class OrderingExample
{
    [SailfishMethod(Order = 3)]
    public void Second()
    {
        Thread.Sleep(50);
    }

    [SailfishMethod(Order = 2)]
    public void First()
    {
        Thread.Sleep(50);
    }

    [SailfishMethod]
    public void Last()
    {
        Thread.Sleep(50);
    }
}