using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace VeerPerforma.Execution
{
    public class VeerTestExecutor : IVeerTestExecutor
    {
        private readonly ILogger logger;
        private readonly IMethodIterator methodIterator;
        private readonly ITestObjectCreator testObjectCreator;

        public VeerTestExecutor(
            ILogger logger,
            ITestObjectCreator testObjectCreator,
            IMethodIterator methodIterator
        )
        {
            this.logger = logger;
            this.testObjectCreator = testObjectCreator;
            this.methodIterator = methodIterator;
        }

        public async Task<int> Execute(Type[] testTypes, Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            try
            {
                foreach (var testType in testTypes) await Execute(testType, callback);
            }
            catch (Exception ex)
            {
                logger.Fatal("The Test runner encountered a fatal error: {0}", ex.Message);
            }

            return await Task.FromResult(0);
        }

        public async Task Execute(Type test, Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            var testInstanceContainers = testObjectCreator.CreateTestContainerInstances(test);
            await Execute(testInstanceContainers);
        }

        public async Task Execute(List<TestInstanceContainer> testInstanceContainers, Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            foreach (var testInstanceContainer in testInstanceContainers) // a test container has all the things need to create a test. All derived from the type.
                await Execute(testInstanceContainer, callback);
        }

        // This will be called in the adapter
        public async Task Execute(TestInstanceContainer testInstanceContainer, Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            var result = await Iterate(testInstanceContainer);
            if (callback is not null)
                callback(testInstanceContainer, result);
        }

        private async Task<TestExecutionResult> Iterate(TestInstanceContainer testInstanceContainer)
        {
            Exception? exception = null;
            var messages = new List<string>();
            try
            {
                await testInstanceContainer.Invocation.GlobalSetup();
                messages = await methodIterator.IterateMethodNTimesAsync(testInstanceContainer);
                await testInstanceContainer.Invocation.GlobalTeardown();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            return exception is null
                ? TestExecutionResult.CreateSuccess(testInstanceContainer, messages)
                : TestExecutionResult.CreateFailure(testInstanceContainer, messages, exception);
        }
    }
}