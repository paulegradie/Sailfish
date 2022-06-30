using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace Sailfish.Execution
{
    public class TestCaseIterator : ITestCaseIterator
    {
        private readonly ILogger logger;

        public TestCaseIterator(ILogger logger)
        {
            this.logger = logger;
        }

        public async Task<List<string>> Iterate(TestInstanceContainer testInstanceContainer)
        {
            await WarmupIterations(testInstanceContainer);

            var messages = new List<string>();
            for (var i = 0; i < testInstanceContainer.NumIterations; i++)
            {
                await testInstanceContainer.Invocation.IterationSetup();

                await testInstanceContainer.Invocation.ExecutionMethod();

                await testInstanceContainer.Invocation.IterationTearDown();
            }

            return messages; // TODO: use this?
        }

        private async Task WarmupIterations(TestInstanceContainer testInstanceContainer)
        {
            for (var i = 0; i < testInstanceContainer.NumWarmupIterations; i++)
            {
                await testInstanceContainer.Invocation.IterationSetup();
                await testInstanceContainer.Invocation.ExecutionMethod();
                await testInstanceContainer.Invocation.IterationTearDown();
            }
        }
    }
}