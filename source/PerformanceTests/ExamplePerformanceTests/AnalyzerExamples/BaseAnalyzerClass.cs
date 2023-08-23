using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.AnalyzerExamples;

public class BaseAnalyzerClass
{
    [SailfishVariable(1, 2, 3)] public int MyVar { get; set; }

    private int myField;
    public string BaseValue { get; set; }

    [SailfishMethodSetup]
    public void ASetup()
    {
        Console.WriteLine("Second");
    }

    protected virtual async Task MyBaseMethod(CancellationToken ct)
    {
        await Task.CompletedTask;
        // do nothing yet
    }

    [SailfishGlobalSetup]
    public async Task BGlobalSetupBaseAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        BaseValue = "WOW!";
        myField = 3;
    }
}