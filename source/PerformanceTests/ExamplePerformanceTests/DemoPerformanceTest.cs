using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[Sailfish(Disabled = TestConstants.Disabled)]
public class DemoPerformanceTest
{
    [SailfishMethod]
    public async Task DoThing(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(100, cancellationToken);
    }
}