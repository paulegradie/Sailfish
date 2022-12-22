using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace AsAConsoleApp.ExamplePerformanceTests;

[Sailfish]
public class DemoPerformanceTest
{
    [SailfishMethod]
    public async Task DoThing(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(100, cancellationToken);
    }
}