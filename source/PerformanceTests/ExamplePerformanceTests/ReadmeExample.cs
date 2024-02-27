using Sailfish.Attributes;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PerformanceTests.ExamplePerformanceTests;

[WriteToMarkdown]
[Sailfish(SampleSize = 3, Disabled = false)]
public class ReadmeExample
{
    private Random random = new();
    [SailfishVariable(1, 10)] public int N { get; set; }

    [SailfishMethod]
    public async Task TestMethod(CancellationToken cancellationToken) // token is injected when requested
    {
        var next = random.Next(50, 450);
        await Task.Delay(next * N, cancellationToken);
    }
}