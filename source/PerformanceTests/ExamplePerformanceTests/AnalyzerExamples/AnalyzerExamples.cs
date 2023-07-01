using System;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.AnalyzerExamples;

[Sailfish]
public class TryToDoSomethingIllegalHere : BaseAnalyzerClass
{
    [SailfishVariable(1, 2, 3)] public int Placeholder { get; set; }


    [SailfishMethod]
    public async Task MainMethod()
    {
        await Task.CompletedTask;
        Console.WriteLine(Placeholder);
    }
}