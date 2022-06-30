using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace Sailfish.Execution
{
    public class SailTestExecutor : ISailTestExecutor
    {
        private readonly ILogger logger;
        private readonly ITestCaseIterator testCaseIterator;
        private readonly ITestInstanceContainerCreator testInstanceContainerCreator;

        public SailTestExecutor(
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

                    // TODO: When we encounter an error on execution, we swallow things for now
                    // Instead, I'd like to return a list of those types that failed, and report their types as well as their exceptions
                    resultsDict.Add(testType, new List<TestExecutionResult>());
                }
            }

            return resultsDict;
        }

        public async Task<List<TestExecutionResult>> Execute(Type test, Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            var testInstanceContainers = testInstanceContainerCreator.CreateTestContainerInstanceProvider(test);
            var results = await Execute(testInstanceContainers);
            return results;
        }

        public async Task<List<TestExecutionResult>> Execute(List<TestInstanceContainerProvider> testInstanceContainerProviders, Action<TestInstanceContainer, TestExecutionResult>? callback = null)
        {
            var results = new List<TestExecutionResult>();

            var enumeratedMethodGroups = testInstanceContainerProviders
                .GroupBy(x => x.method.Name)
                .Zip(Enumerable.Range(0, testInstanceContainerProviders.Count));

            var previousMethod = 0;
            foreach (var (instanceProviders, currentMethod) in enumeratedMethodGroups)
            {
                foreach (var (provider, instanceIndex) in instanceProviders.Zip(Enumerable.Range(0, instanceProviders.Count())))
                {
                    var instanceContainer = provider.ProvideTestInstanceContainer();
                    if (instanceIndex == 0 && currentMethod == 0)
                    {
                        await instanceContainer.Invocation.GlobalSetup();
                    }

                    if (instanceIndex == 0 && (currentMethod == 0 || previousMethod != currentMethod))
                    {
                        await instanceContainer.Invocation.MethodSetup();
                    }


                    var executionResult = await Execute(instanceContainer, callback);
                    results.Add(executionResult);

                    if (instanceIndex == instanceProviders.Count() - 1)
                    {
                        await instanceContainer.Invocation.MethodTearDown();
                    }

                    if (currentMethod == enumeratedMethodGroups.Count() - 1 && instanceIndex == instanceProviders.Count() - 1)
                    {
                        await instanceContainer.Invocation.GlobalTeardown();
                    }

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

                previousMethod = currentMethod;
            }

            return results;
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