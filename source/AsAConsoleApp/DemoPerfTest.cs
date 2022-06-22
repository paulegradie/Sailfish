using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Test.API;
using Test.ApiCommunicationTests.Base;
using VeerPerforma.Attributes;

namespace AsAConsoleApp
{
    [WriteToMarkdown]
    [WriteToCsv]
    [VeerPerforma(1)]
    public class DemoPerfTest : ApiTestBase
    {
        public DemoPerfTest(WebApplicationFactory<DemoApp> factory) : base(factory)
        {
        }

        [IterationVariable(1, 2)] public int NTries { get; set; }

        [IterationVariable(200, 300)] public int WaitPeriod { get; set; }

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


        [ExecutePerformanceCheck]
        public async Task WaitPeriodPerfTest()
        {
            Thread.Sleep(WaitPeriod);
            await Client.GetStringAsync("/");
            WriteSomething();
        }

        [ExecutePerformanceCheck]
        public async Task Other()
        {
            Thread.Sleep(1);
            await Task.CompletedTask;
            Console.WriteLine("WOW");
        }

        private void WriteSomething()
        {
            Console.WriteLine($"Wait Period - Iteration Complete: {NTries}-{WaitPeriod}");
        }
    }
}