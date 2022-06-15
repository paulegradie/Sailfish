using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace VeerPerforma.Execution
{
    public class MethodIterator : IMethodIterator
    {
        private readonly ILogger logger;

        public MethodIterator(ILogger logger)
        {
            this.logger = logger;
        }

        public async Task<List<string>> IterateMethodNTimesAsync(TestInstanceContainer testInstanceContainer)
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