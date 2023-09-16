using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToMarkdown]
[Sailfish(SampleSize = 2, Disabled = false)]
public class ScaleFishExample
{
    [SailfishRangeVariable(true, start: 5, 4, 6)]
    public int N { get; set; }


    [SailfishMethod(Disabled = true)]
    public async Task Quadratic(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * N), cancellationToken);
    }

    [SailfishMethod(Disabled = true)]
    public async Task Cubic(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * N * N), cancellationToken);
    }

    [SailfishMethod(Disabled = false, DisableOverheadEstimation = true)]
    public async Task NLogN(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * Math.Log(N)), cancellationToken);
    }
}