using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[WriteToMarkdown]
[Sailfish(Disabled = false)]
public class ComplexityEstimation
{
    [SailfishVariable(true, 1, 2, 3, 4)] public int N { get; set; }
    [SailfishVariable(true, 1, 2, 3)] public int OtherN { get; set; }
    [SailfishVariable(true, 1, 2)] public int ThirdN { get; set; }

    [SailfishMethod]
    public async Task DoThing(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(1, cancellationToken);
    }
}