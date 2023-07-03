using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.AnalyzerExamples;

[Sailfish]
public class TryToDoSomethingIllegalHere : BaseAnalyzerClass
{
    [SailfishVariable(1, 2, 3)] public int Placeholder { get; set; }

    public string MustHavePublicSettersAndGettersAndPublicModifier { get; set; } = null!;

    [SailfishGlobalSetup]
    public async Task AGlobalSetupAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        MustHavePublicSettersAndGettersAndPublicModifier = "WOW!";
    }

    [SailfishMethodSetup]
    public void BSetup()
    {
        Console.WriteLine("FIRST");
    }

    [SailfishMethod]
    public async Task MainMethod()
    {
        await Task.CompletedTask;
        Console.WriteLine(Placeholder);
    }
}