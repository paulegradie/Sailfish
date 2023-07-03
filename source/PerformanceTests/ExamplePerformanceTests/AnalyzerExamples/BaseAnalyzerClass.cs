using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.AnalyzerExamples;

public class BaseAnalyzerClass
{
    [SailfishVariable(1, 2, 3)] public int MyVar { get; set; }


    private int myField;

    public string BaseValue { get; set; }

    [SailfishGlobalSetup]
    public async Task GlobalSetupBaseAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        BaseValue = "WOW!";
        myField = 3;
    }
}