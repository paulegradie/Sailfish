using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace Sailfish.Execution
{
    internal class SailFishTestExecutor : ISailFishTestExecutor
    {
        private readonly ILogger logger;
        private readonly ITestCaseIterator testCaseIterator;
        private readonly ITestInstanceContainerCreator testInstanceContainerCreator;

        public SailFishTestExecutor(
            ILogger logger,
            ITestInstanceContainerCreator testInstanceContainerCreator,
            ITestCaseIterator testCaseIterator
        )
        {
            this.logger = logger;
            this.testInstanceContainerCreator = testInstanceContainerCreator;
            this.testCaseIterator = testCaseIterator;
        }

        public async Task<List<RawExecutionResult>> Execute(
            Type[] testTypes,
            Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            var rawResults = new List<RawExecutionResult>();
            foreach (var testType in testTypes)
            {
                try
                {
                    var rawResult = await Execute(testType, callback);
                    rawResults.Add(new RawExecutionResult(testType, rawResult));
                }
                catch (Exception ex)
                {
                    logger.Fatal("The Test runner encountered a fatal error: {0}", ex.Message);
                    rawResults.Add(new RawExecutionResult(testType, ex));
                }
            }

            return rawResults;
        }

        public async Task<List<TestExecutionResult>> Execute(Type test, Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            var testInstanceContainers = testInstanceContainerCreator.CreateTestContainerInstanceProviders(test);
            var results = await Execute(testInstanceContainers);
            return results;
        }

        public async Task<List<TestExecutionResult>> Execute(
            List<TestInstanceContainerProvider> testMethods,
            Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            var results = new List<TestExecutionResult>();

            var methodIndex = 0;
            var totalMethodCount = testMethods.Count() - 1;
            foreach (var testMethod in testMethods.OrderBy(x => x.Method.Name))
            {
                var currentVariableSetIndex = 0;
                var totalNumVariableSets = testMethod.GetNumberOfVariableSetsInTheQueue() - 1;

                foreach (var testMethodContainer in testMethod.ProvideNextTestInstanceContainer())
                {
                    if (ShouldCallGlobalSetup(methodIndex, currentVariableSetIndex))
                    {
                        await testMethodContainer.Invocation.GlobalSetup();
                    }

                    await testMethodContainer.Invocation.MethodSetup();
                    var executionResult = await Execute(testMethodContainer, callback);
                    results.Add(executionResult);
                    await testMethodContainer.Invocation.MethodTearDown();

                    if (ShouldCallGlobalTeardown(methodIndex, totalMethodCount, currentVariableSetIndex, totalNumVariableSets))
                    {
                        await testMethodContainer.Invocation.GlobalTeardown();
                    }

                    if (ShouldDisposeOfInstance(currentVariableSetIndex, totalNumVariableSets))
                    {
                        await DisposeOfTestInstance(testMethodContainer);
                    }

                    currentVariableSetIndex += 1;
                }

                methodIndex += 1;
            }

            return results;
        }

        private static async Task DisposeOfTestInstance(TestInstanceContainer instanceContainer)
        {
            if (instanceContainer.Instance is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else if (instanceContainer.Instance is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                instanceContainer.Instance = null!;
            }
        }

        // This will be called in the adapter
        public async Task<TestExecutionResult> Execute(TestInstanceContainer container, Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            var result = await Iterate(container);
            if (callback is not null)
            {
                callback(result.TestInstanceContainer, result);
            }

            return result;
        }

        private async Task<TestExecutionResult> Iterate(TestInstanceContainer testInstanceContainer)
        {
            try
            {
                var messages = await testCaseIterator.Iterate(testInstanceContainer);
                return new TestExecutionResult(testInstanceContainer, messages);
            }
            catch (Exception exception)
            {
                return new TestExecutionResult(testInstanceContainer, exception);
            }
        }

        private static bool ShouldCallGlobalTeardown(int methodIndex, int totalMethodCount, int currentVariableSetIndex, int totalNumVariableSets)
        {
            return methodIndex == totalMethodCount && currentVariableSetIndex == totalNumVariableSets;
        }

        private static bool ShouldDisposeOfInstance(int currentVariableSetIndex, int totalNumVariableSets)
        {
            return currentVariableSetIndex == totalNumVariableSets;
        }

        private static bool ShouldCallGlobalSetup(int methodIndex, int currentMethodIndex)
        {
            return methodIndex == 0 && currentMethodIndex == 0;
        }
    }
}