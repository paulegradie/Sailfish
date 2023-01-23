using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Sailfish.Attributes;
using Test.API;

// Tests here are automatically discovered and executed
namespace AsAConsoleApp.ExamplePerformanceTests;

[WriteToMarkdown]
[WriteToCsv]
[Sailfish(5, 2, Disabled = false)]
public class ExamplePerformanceTest : TestBase
{
    public ExamplePerformanceTest(WebApplicationFactory<DemoApp> factory) : base(factory)
    {
    }

    [SailfishVariable(200, 300)] public int WaitPeriod { get; set; }
    [SailfishVariable(1, 2)] public int NTries { get; set; } // try to avoid multiple variables if you can manage

    [SailfishGlobalSetup]
    public void GlobalSetup(CancellationToken cancellationToken)
    {
        Console.WriteLine("This is the Global Setup");
    }

    [SailfishMethodSetup]
    public void ExecutionMethodSetup(CancellationToken cancellationToken)
    {
        Console.WriteLine("This is the Execution Method Setup");
    }

    [SailfishIterationSetup]
    public void IterationSetup(CancellationToken cancellationToken)
    {
        Console.WriteLine("This is the Iteration Setup - use sparingly");
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
    public void IterationTeardown(CancellationToken cancellationToken)
    {
        Console.WriteLine("This is the Iteration Teardown - use sparingly");
    }

    [SailfishMethodTeardown]
    public void ExecutionMethodTeardown(CancellationToken cancellationToken)
    {
        Console.WriteLine("This is the Execution Method Teardown");
    }

    [SailfishGlobalTeardown]
    public override async Task GlobalTeardown(CancellationToken cancellationToken)
    {
        await Task.Yield();
        Console.WriteLine("This is the Global Teardown");
    }
}