using Microsoft.AspNetCore.Mvc.Testing;
using Test.API.Controllers;
using Test.ApiCommunicationTests.Base;
using VeerPerforma.Attributes;
using VeerPerforma.Attributes.TestHarness;

namespace PerfTestProjectDemo;

[VeerPerforma]
public class CountToAMillionPerformance : ApiTestBase
{
    [IterationVariable(1, 2, 3)]
    public int NTries { get; set; }

    [IterationVariable(200, 400, 1200)]
    public int WaitPeriod { get; set; }

    [VeerGlobalSetup]
    public void GlobalSetup()
    {
    }

    [VeerGlobalTeardown]
    public void GlobalTeardown()
    {
    }

    [VeerExecutionMethodSetup]
    public void ExecutionMethodSetup()
    {
    }

    [VeerExecutionMethodTeardown]
    public void ExecutionMethodTeardown()
    {
    }

    [VeerExecutionIterationSetup]
    public void IterationSetup()
    {
    }

    [VeerExecutionIterationTeardown]
    public void IterationTeardown()
    {
    }

    [ExecutePerformanceCheck]
    public async Task NTriesPerfTest() // must be parameterless
    {
        Console.WriteLine("Executing NTries Perf Test");
        for (var i = 0; i < NTries; i++) // don't need to provide all property variables in each execution method
        {
            await Client.GetStringAsync(CountToTenMillionController.Route);
            Console.WriteLine($"NTries - Iteration Complete: {NTries}-{i}");
        }
    }

    [ExecutePerformanceCheck]
    public async Task WaitPeriodPerfTest()
    {
        Console.WriteLine("Executing WaitPeriod Perf Test");
        for (var i = 0; i < NTries; i++)
        {
            Thread.Sleep(WaitPeriod);
            await Client.GetStringAsync("/");
            Console.WriteLine($"Wait Period - Iteration Complete: {NTries}-{i}");

        }
    }

    public CountToAMillionPerformance(WebApplicationFactory<MyApp> factory) : base(factory)
    {
    }
}