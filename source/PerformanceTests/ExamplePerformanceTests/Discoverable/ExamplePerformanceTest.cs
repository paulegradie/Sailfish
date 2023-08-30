using System.Threading;
using System.Threading.Tasks;
using Demo.API;
using Microsoft.AspNetCore.Mvc.Testing;
using PerformanceTests.DemoUtils;
using Sailfish.Attributes;
using Serilog;

// Tests here are automatically discovered and executed
namespace PerformanceTests.ExamplePerformanceTests.Discoverable;

[WriteToMarkdown]
[WriteToCsv]
[Sailfish]
public class ExamplePerformanceTest : TestBase
{
    public ExamplePerformanceTest(
        WebApplicationFactory<DemoApp> factory,
        ILogger logger) : base(factory)
    {
    }

    [SailfishVariable(true, 20, 50, 100, 200, 300)]
    public int WaitPeriod { get; set; }

    [SailfishVariable(1, 2)] 
    public int NTries { get; set; }

    [SailfishMethod(DisableOverheadEstimation = true)]
    public async Task WaitPeriodPerfTest(CancellationToken ct)
    {
        await Task.Delay(WaitPeriod, ct);
        await Client.GetStringAsync("/", ct);
    }

    [SailfishMethod(DisableOverheadEstimation = true)]
    public async Task Other(CancellationToken cancellationToken)
    {
        await Task.Delay(WaitPeriod, cancellationToken);
    }
}