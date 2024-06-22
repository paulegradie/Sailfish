using System;
using System.Threading;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(SampleSize = 3, DisableOverheadEstimation = true, NumWarmupIterations = 0)]
public class OrderingExample
{
    [SailfishMethod(Order = 2)]
    public void Second()
    {
        throw new Exception();
    }

    [SailfishMethod(Order = 1)]
    public void First()
    {
        Thread.Sleep(1_000);
    }

    [SailfishMethod(Order = 3)]
    public void Last()
    {
        Thread.Sleep(1_000);
    }
}