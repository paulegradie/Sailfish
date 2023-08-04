using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[WriteToMarkdown]
[Sailfish(Disabled = false)]
public class ComplexityEstimationPerformanceTest
{
    [SailfishVariable(true, 1, 5, 10, 20, 50, 100)] public int N { get; set; }

    [SailfishVariable(1, 2)] public int OtherN { get; set; }

    [SailfishMethod]
    public async Task MeasureForComplexity_Quadratic(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * N) + OtherN, cancellationToken);
    }

    [SailfishMethod]
    public async Task MeasureForComplexity_Cubic(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * N * N) + OtherN, cancellationToken);
    }

    [SailfishMethod]
    public async Task MeasureForComplexity_NLogN(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        await Task.Delay(Convert.ToInt32(N * Math.Log(N)) + OtherN, cancellationToken);
    }
}