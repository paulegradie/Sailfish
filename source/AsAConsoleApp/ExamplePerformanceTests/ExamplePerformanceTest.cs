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
[Sailfish(1, 2, Disabled = false)]
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

    [SailfishGlobalTeardown]
    public void GlobalTeardown()
    {
        Console.WriteLine("This is the Global Teardown");
    }

    [SailfishMethodSetup]
    public void ExecutionMethodSetup()
    {
        Console.WriteLine("This is the Execution Method Setup");
    }

    [SailfishMethodTeardown]
    public void ExecutionMethodTeardown()
    {
        Console.WriteLine("This is the Execution Method Teardown");
    }

    [SailfishIterationSetup]
    public void IterationSetup()
    {
        Console.WriteLine("This is the Iteration Setup - use sparingly");
    }

    [SailfishIterationTeardown]
    public void IterationTeardown()
    {
        Console.WriteLine("This is the Iteration Teardown - use sparingly");
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
}