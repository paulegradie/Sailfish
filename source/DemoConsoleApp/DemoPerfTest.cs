using Microsoft.AspNetCore.Mvc.Testing;
using Test.API;
using Test.ApiCommunicationTests.Base;
using VeerPerforma.Attributes;
using VeerPerforma.Attributes.TestHarness;

namespace PerfTestProjectDemo;

[VeerPerforma(2)]
public class DemoPerfTest : ApiTestBase
{
    [VeerGlobalSetup]
    public void GlobalSetup()
    {
        Console.WriteLine("This is the Global Setup");
    }

    [VeerGlobalTeardown]
    public void GlobalTeardown()
    {
        Console.WriteLine("This is the Global Teardown");
    }

    [VeerExecutionMethodSetup]
    public void ExecutionMethodSetup()
    {
        Console.WriteLine("This is the Execution Method Setup");
    }

    [VeerExecutionMethodTeardown]
    public void ExecutionMethodTeardown()
    {
        Console.WriteLine("This is the Execution Method Teardown");
    }

    [VeerExecutionIterationSetup]
    public void IterationSetup()
    {
        Console.WriteLine("This is the Iteration Setup - use sparingly");
    }

    [VeerExecutionIterationTeardown]
    public void IterationTeardown()
    {
        Console.WriteLine("This is the Iteration Teardown - use sparingly");
    }

    [IterationVariable(1, 2, 3)]
    public int NTries { get; set; }

    [IterationVariable(200, 300)]
    public int WaitPeriod { get; set; }
    

    [ExecutePerformanceCheck]
    public async Task WaitPeriodPerfTest()
    {
        var test = new TestCollector();
        Thread.Sleep(WaitPeriod);
        await Client.GetStringAsync("/");
        WriteSomething();
    }

    private void WriteSomething()
    {
        Console.WriteLine($"Wait Period - Iteration Complete: {NTries}-{WaitPeriod}");
    }

    public DemoPerfTest(WebApplicationFactory<MyApp> factory) : base(factory)
    {
    }
}