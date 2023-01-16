using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.ExtensionMethods;
using Serilog;

namespace Sailfish.Execution;

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

    private static bool FilterEnabledType(IEnumerable<Type> testTypes, out Type[] enabledTypes)
    {
        enabledTypes = testTypes.Where(x => !x.SailfishTypeIsDisabled()).ToArray();
        return enabledTypes.Length > 0;
    }

    public async Task<List<RawExecutionResult>> Execute(
        IEnumerable<Type> testTypes,
        Action<TestExecutionResult>? callback = null,
        CancellationToken cancellationToken = default)
    {
        var rawResults = new List<RawExecutionResult>();
        if (!FilterEnabledType(testTypes, out var enabledTestTypes))
        {
            logger.Warning("No Sailfish tests were discovered...");
            return rawResults;
        }

        var testIndex = 0;
        var totalTestCount = enabledTestTypes.Length;

        logger.Information("Discovered {TotalTestCount} enabled test types", totalTestCount);
        foreach (var testType in enabledTestTypes)
        {
            try
            {
                logger.Information("Executing test type {TestIndex} of {TotalTestCount}: {TestName}", testIndex + 1, totalTestCount, testType.Name);
                var rawResult = await Execute(testType, callback, cancellationToken);
                rawResults.Add(new RawExecutionResult(testType, rawResult));
            }
            catch (Exception ex)
            {
                logger.Fatal("The Test runner encountered a fatal error: {Message}", ex.Message);
                rawResults.Add(new RawExecutionResult(testType, ex));
            }

            testIndex += 1;
        }

        return rawResults;
    }

    public async Task<List<TestExecutionResult>> Execute(
        Type test,
        Action<TestExecutionResult>? callback = null,
        CancellationToken cancellationToken = default)
    {
        var testInstanceContainers = testInstanceContainerCreator.CreateTestContainerInstanceProviders(test);
        var results = await Execute(testInstanceContainers, callback, cancellationToken);
        return results;
    }

    private async Task<List<TestExecutionResult>> Execute(
        IReadOnlyCollection<TestInstanceContainerProvider> testMethods,
        Action<TestExecutionResult>? callback = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TestExecutionResult>();

        var methodIndex = 0;
        var totalMethodCount = testMethods.Count - 1;
        foreach (var testMethod in testMethods.OrderBy(x => x.Method.Name))
        {
            logger.Information(
                "Executing test method {MethodIndex} of {TotalMethodCount}: {TestTypeName}.{TestMethodName}",
                methodIndex + 1, totalMethodCount + 1, testMethod.Method.DeclaringType?.Name, testMethod.Method.Name);
            var currentVariableSetIndex = 0;
            var totalNumVariableSets = testMethod.GetNumberOfVariableSetsInTheQueue() - 1;

            var instanceContainerEnumerator = testMethod.ProvideNextTestInstanceContainer().GetEnumerator();

            bool cont;
            try
            {
                instanceContainerEnumerator.MoveNext();
            }
            catch (Exception ex)
            {
                await DisposeOfTestInstance(instanceContainerEnumerator.Current);
                instanceContainerEnumerator.Dispose();
                logger.Fatal(ex, "Error encountered when getting next test");
                throw;
            }

            do
            {
                var testMethodContainer = instanceContainerEnumerator.Current;

                if (ShouldCallGlobalSetup(methodIndex, currentVariableSetIndex))
                {
                    await testMethodContainer.Invocation.GlobalSetup(cancellationToken);
                }

                await testMethodContainer.Invocation.MethodSetup(cancellationToken);

                var executionResult = await Execute(testMethodContainer, callback, cancellationToken);
                results.Add(executionResult);

                await testMethodContainer.Invocation.MethodTearDown(cancellationToken);

                if (ShouldCallGlobalTeardown(methodIndex, totalMethodCount, currentVariableSetIndex, totalNumVariableSets))
                {
                    await testMethodContainer.Invocation.GlobalTeardown(cancellationToken);
                }

                if (ShouldDisposeOfInstance(currentVariableSetIndex, totalNumVariableSets))
                {
                    await DisposeOfTestInstance(testMethodContainer);
                }

                currentVariableSetIndex += 1;

                try
                {
                    cont = instanceContainerEnumerator.MoveNext();
                }
                catch
                {
                    await DisposeOfTestInstance(instanceContainerEnumerator.Current);
                    throw;
                }
            } while (cont);

            methodIndex += 1;
            instanceContainerEnumerator.Dispose();
        }

        return results;
    }

    private static async Task DisposeOfTestInstance(TestInstanceContainer? instanceContainer)
    {
        switch (instanceContainer?.Instance)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
            default:
            {
                if (instanceContainer is not null)
                {
                    instanceContainer.Instance = null!;
                }

                break;
            }
        }
    }

    // This will be called in the adapter
    public async Task<TestExecutionResult> Execute(
        TestInstanceContainer container,
        Action<TestExecutionResult>? callback = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Iterate(container, cancellationToken);
        callback?.Invoke(result);

        return result;
    }

    private async Task<TestExecutionResult> Iterate(TestInstanceContainer testInstanceContainer, CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await testCaseIterator.Iterate(testInstanceContainer, cancellationToken);
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