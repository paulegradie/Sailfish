using System.Threading;
using System.Threading.Tasks;
using PerformanceTests.DemoUtils;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(Disabled = true)]
public class DemoPerformanceTest
{
    [SailfishMethod]
    public async Task DoThing(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(100, cancellationToken);
    }
}