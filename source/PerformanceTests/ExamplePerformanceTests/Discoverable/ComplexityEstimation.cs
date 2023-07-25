using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[WriteToMarkdown]
[Sailfish(Disabled = false)]
public class ComplexityEstimation
{
    [SailfishVariable(true, 1, 4, 6)] public int N { get; set; }
    [SailfishVariable(true, 1, 4, 6)] public int OtherN { get; set; }
    [SailfishVariable(1, 2)] public int SomeVar { get; set; }


    [SailfishMethod]
    public async Task DoThing(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var val = (N * Math.Log(N)) + (OtherN * OtherN);
        await Task.Delay(Convert.ToInt32(val * 100), cancellationToken);
    }
}