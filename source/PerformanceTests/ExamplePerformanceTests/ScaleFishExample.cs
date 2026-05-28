using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToMarkdown]
[Sailfish(SampleSize = 20, DisableOverheadEstimation = true, Disabled = false)]
public class ScaleFishExample
{
    // Geometric (log-spaced) values — recommended for ScaleFish complexity probes.
    // Produces N ∈ {5, 10, 21, 44, 90, 184}: equally spaced in log-x for maximum discrimination.
    [SailfishRangeVariable(scaleFish: true, start: 5, end: 184, count: 6, spacing: RangeSpacing.Geometric)]
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