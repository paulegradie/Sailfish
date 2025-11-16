using System;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToMarkdown]
[Sailfish(SampleSize = 3, Disabled = false)]
public class ReadmeExample
{
    private readonly Random _random = new();

    [SailfishVariable(1, 2)]
    public int N { get; set; }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken cancellationToken) // token is injected when requested
    {
        var next = _random.Next(50, 450);
        await Task.Delay(next * N, cancellationToken);
    }
}