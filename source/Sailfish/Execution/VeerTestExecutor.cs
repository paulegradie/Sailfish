using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace Sailfish.Execution
{
    public class VeerTestExecutor : IVeerTestExecutor
    {
        private readonly ILogger logger;
        private readonly ITestCaseIterator testCaseIterator;
        private readonly ITestInstanceContainerCreator testInstanceContainerCreator;

        public VeerTestExecutor(
            ILogger logger,
            ITestInstanceContainerCreator testInstanceContainerCreator,
            ITestCaseIterator testCaseIterator
        )
        {
            this.logger = logger;
            this.testInstanceContainerCreator = testInstanceContainerCreator;
            this.testCaseIterator = testCaseIterator;
        }

        public async Task<Dictionary<Type, List<TestExecutionResult>>> Execute(Type[] testTypes, Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            var resultsDict = new Dictionary<Type, List<TestExecutionResult>>();
            foreach (var testType in testTypes)
            {
                try
                {
                    var executionResults = await Execute(testType, callback);
                    resultsDict.Add(testType, executionResults);
                }
                catch (Exception ex)
                {
                    logger.Fatal("The Test runner encountered a fatal error: {0}", ex.Message);
                    resultsDict.Add(testType, new List<TestExecutionResult>());
                }
            }

            return resultsDict;
        }

        public async Task<List<TestExecutionResult>> Execute(Type test, Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            var testInstanceContainers = testInstanceContainerCreator.CreateTestContainerInstances(test);
            var results = await Execute(testInstanceContainers);
            return results;
        }

        public async Task<List<TestExecutionResult>> Execute(List<TestInstanceContainer> testInstanceContainers, Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            var results = new List<TestExecutionResult>();
            foreach (var testInstanceContainer in testInstanceContainers) // a test container has all the things need to create a test. All derived from the type.
            {
                var executionResult = await Execute(testInstanceContainer, callback);
                results.Add(executionResult);
            }

            return results;
        }

        // This will be called in the adapter
        public async Task<TestExecutionResult> Execute(TestInstanceContainer testInstanceContainer, Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            var result = await Iterate(testInstanceContainer);
            if (callback is not null)
            {
                callback(testInstanceContainer, result);
            }

            return result;
        }

        private async Task<TestExecutionResult> Iterate(TestInstanceContainer testInstanceContainer)
        {
            Exception? exception = null;
            var messages = new List<string>();
            try
            {
                messages = await testCaseIterator.Iterate(testInstanceContainer);
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