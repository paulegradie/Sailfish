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
    private readonly ILogger logger;

    public ExamplePerformanceTest(WebApplicationFactory<DemoApp> factory, ILogger logger) : base(factory)
    {
        this.logger = logger;
    }

    [SailfishVariable(200, 300)] public int WaitPeriod { get; set; }
    [SailfishVariable(1, 2)] public int NTries { get; set; } // try to avoid multiple variables if you can manage

    [SailfishGlobalSetup]
    public void GlobalSetup()
    {
        // logger.Information("This is the Global Setup");
    }

    [SailfishMethodSetup]
    public void ExecutionMethodSetup()
    {
        // logger.Information("This is the Execution Method Setup");
    }

    [SailfishIterationSetup(nameof(WaitPeriodPerfTest))]
    public void IterationSetup()
    {
        // logger.Warning("This is the Iteration Setup - use sparingly");
    }

    [SailfishMethod]
    public async Task WaitPeriodPerfTest(CancellationToken cancellationToken)
    {
        await Task.Delay(WaitPeriod, cancellationToken);
        await Client.GetStringAsync("/", cancellationToken);
    }

    [SailfishMethod]
    public async Task Other(CancellationToken cancellationToken)
    {
        await Task.Delay(WaitPeriod, cancellationToken);
        await Task.CompletedTask;
    }

    [SailfishIterationTeardown]
    public void IterationTeardown()
    {
        // logger.Warning("This is the Iteration Teardown - use sparingly");
    }

    [SailfishMethodTeardown]
    public void ExecutionMethodTeardown()
    {
        // logger.Verbose("This is the Execution Method Teardown");
    }

    [SailfishGlobalTeardown]
    public override async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        await Task.Yield();
    }
}