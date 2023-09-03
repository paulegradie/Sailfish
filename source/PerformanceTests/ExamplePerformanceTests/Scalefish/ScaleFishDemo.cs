using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Scalefish;

[WriteToMarkdown]
[Sailfish(NumIterations = 7, Disabled = false)]
public class ScaleFishDemo
{
    [SailfishRangeVariable(true, 1, 6, 30)]
    public int N { get; set; }

    [SailfishVariable(50, 100)] public int OtherN { get; set; }

    [SailfishMethod(Disabled = true)]
    public async Task Quadratic(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * N) + OtherN, cancellationToken);
    }

    [SailfishMethod(Disabled = true)]
    public async Task Cubic(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * N * N) + OtherN, cancellationToken);
    }

    [SailfishMethod(Disabled = false, DisableOverheadEstimation = true)]
    public async Task NLogN(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * Math.Log(N)) + OtherN, cancellationToken);
    }
}