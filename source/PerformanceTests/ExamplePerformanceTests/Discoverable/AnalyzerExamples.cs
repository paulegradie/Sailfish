using System;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish]
public class AnalyzerExample
{
    [SailfishVariable(1, 2, 3)] private int Placeholder { get; set; }

    [SailfishMethod]
    public void MainMethod()
    {
        Console.WriteLine(Placeholder);
    }
}