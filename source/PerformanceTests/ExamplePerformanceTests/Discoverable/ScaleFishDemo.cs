using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[WriteToMarkdown]
[Sailfish(NumIterations = 7, Disabled = false)]
public class ScaleFishDemo
{
    [SailfishVariableRange(true, 1, 6, 30)]
    public int N { get; set; }

    [SailfishVariable(50, 100)] public int OtherN { get; set; }

    [SailfishMethod(disabled: true)]
    public async Task Quadratic(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * N) + OtherN, cancellationToken);
    }

    [SailfishMethod(disabled: true)]
    public async Task Cubic(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * N * N) + OtherN, cancellationToken);
    }

    [SailfishMethod(disabled: false, disableOverheadEstimation: true)]
    public async Task NLogN(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * Math.Log(N)) + OtherN, cancellationToken);
    }
}