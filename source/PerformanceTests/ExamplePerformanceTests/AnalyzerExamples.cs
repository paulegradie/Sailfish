using System;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[Sailfish]
public class AnalyzerExample
{
    [SailfishVariable(1, 2, 3)] 
    public int Placeholder { get; set; }

    [SailfishMethod]
    public void MainMethod()
    {
        Console.WriteLine(Placeholder);
    }
}