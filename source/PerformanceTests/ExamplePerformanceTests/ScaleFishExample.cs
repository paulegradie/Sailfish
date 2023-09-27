using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToMarkdown]
[Sailfish(SampleSize = 2, Disabled = false)]
public class ScaleFishExample
{
    [SailfishRangeVariable(true, start: 5, 10, 6)]
    public int N { get; set; }

    [SailfishMethod]
    public async Task Linear(CancellationToken ct)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt16(12.5 * N + 3), ct);
    }

    [SailfishMethod(Disabled = true)]
    public async Task Quadratic(CancellationToken ct)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * N), ct);
    }

    [SailfishMethod(Disabled = false, DisableOverheadEstimation = true)]
    public async Task NLogN(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * Math.Log(N)), cancellationToken);
    }
}