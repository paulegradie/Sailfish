using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Test.API;
using Test.ApiCommunicationTests.Base;
using Sailfish.Attributes;

namespace AsAConsoleApp
{
    [WriteToMarkdown]
    [WriteToCsv]
    [Sailfish(1)]
    public class DemoPerfTest : ApiTestBase
    {
        public DemoPerfTest(WebApplicationFactory<DemoApp> factory) : base(factory)
        {
        }

        [IterationVariable(1, 2)]
        public int NTries { get; set; }

        [IterationVariable(200, 300)]
        public int WaitPeriod { get; set; }

        [SailGlobalSetup]
        public void GlobalSetup()
        {
            Console.WriteLine("This is the Global Setup");
        }

        [SailGlobalTeardown]
        public void GlobalTeardown()
        {
            Console.WriteLine("This is the Global Teardown");
        }

        [SailExecutionMethodSetup]
        public void ExecutionMethodSetup()
        {
            Console.WriteLine("This is the Execution Method Setup");
        }

        [SailExecutionMethodTeardown]
        public void ExecutionMethodTeardown()
        {
            Console.WriteLine("This is the Execution Method Teardown");
        }

        [SailExecutionIterationSetup]
        public void IterationSetup()
        {
            Console.WriteLine("This is the Iteration Setup - use sparingly");
        }

        [SailExecutionIterationTeardown]
        public void IterationTeardown()
        {
            Console.WriteLine("This is the Iteration Teardown - use sparingly");
        }


        [ExecutePerformanceCheck]
        public async Task WaitPeriodPerfTest()
        {
            await Task.Delay(WaitPeriod);
            await Client.GetStringAsync("/");
        }

        [ExecutePerformanceCheck]
        public async Task Other()
        {
            Thread.Sleep(200);
            await Task.CompletedTask;
            Console.WriteLine("WOW");
        }
    }
}