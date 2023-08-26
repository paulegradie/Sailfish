using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[WriteToMarkdown]
[Sailfish(NumIterations = 5, Disabled = false)]
public class DemoPerformanceTest
{
    [SailfishVariable(true, 10, 15, 50, 100)] 
    public int MyInts { get; set; }

    [SailfishMethod]
    public async Task DoThing(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(MyInts, cancellationToken);
    }
}