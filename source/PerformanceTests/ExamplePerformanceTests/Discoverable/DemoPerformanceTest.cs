using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[Sailfish(Disabled = false)]
public class DemoPerformanceTest
{
    [SailfishVariable(1, 4, 6)] public int MyInts { get; set; }
    [SailfishVariable(1, 4, 6)] public int MyIntsTwo { get; set; }

    [SailfishMethod]
    public async Task DoThing(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(100, cancellationToken);
    }
}