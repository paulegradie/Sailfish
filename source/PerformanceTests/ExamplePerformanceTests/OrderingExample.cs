using Sailfish.Attributes;
using System;
using System.Threading;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToCsv]
[WriteToMarkdown]
[Sailfish(SampleSize = 1, DisableOverheadEstimation = true, NumWarmupIterations = 0)]
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
        Thread.Sleep(8000);
    }

    [SailfishMethod(Order = 3)]
    public void Last()
    {
        Thread.Sleep(8000);
    }
}