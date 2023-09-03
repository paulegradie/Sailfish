using System;
using System.Threading;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(3, 0, Disabled = false)]
public class RangeAttributeExample
{
    [SailfishRangeVariable(true, 0, 30, 2)]
    public int N { get; set; }

    [SailfishMethod(Disabled = false, DisableOverheadEstimation = true )]
    public void MainTestMethod()
    {
        Console.WriteLine(N);
        Thread.Sleep(N);
    }
}