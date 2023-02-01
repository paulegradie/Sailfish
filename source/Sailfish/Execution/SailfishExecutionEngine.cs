using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Program;
using Sailfish.Registration;
using Serilog;

namespace Sailfish.Execution;

internal class SailfishExecutionEngine : ISailfishExecutionEngine
{
    private readonly ITestCaseIterator testCaseIterator;

    public SailfishExecutionEngine(ITestCaseIterator testCaseIterator)
    {
        this.testCaseIterator = testCaseIterator;
    }

    public async Task<List<TestExecutionResult>> ActivateContainer(
        [Range(0, int.MaxValue)] int testProviderIndex,
        [Range(1, int.MaxValue)] int totalTestProviderCount,
        TestInstanceContainerProvider testProvider,
        Action<TestExecutionResult>? callback = null,
        CancellationToken cancellationToken = default)
    {
        var currentVariableSetIndex = 0;
        var totalNumVariableSets = testProvider.GetNumberOfPropertySetsInTheQueue() - 1;

        var instanceContainerEnumerator = testProvider.ProvideNextTestInstanceContainer().GetEnumerator();

        try
        {
            instanceContainerEnumerator.MoveNext();
        }
        catch (Exception ex)
        {
            await DisposeOfTestInstance(instanceContainerEnumerator.Current);
            instanceContainerEnumerator.Dispose();
            Log.Logger.Fatal(ex, "Error encountered when getting next test");
            throw;
        }

        var results = new List<TestExecutionResult>();
        bool continueIterating;
        do
        {

            var testMethodContainer = instanceContainerEnumerator.Current;
            TestCaseCountPrinter.PrintCaseUpdate(testMethodContainer.TestCaseId.DisplayName);

            if (ShouldCallGlobalSetup(testProviderIndex, currentVariableSetIndex))
            {
                await testMethodContainer.Invocation.GlobalSetup(cancellationToken);
            }

            await testMethodContainer.Invocation.MethodSetup(cancellationToken);

            var executionResult = await IterateOverVariableCombos(testMethodContainer, cancellationToken);

            await testMethodContainer.Invocation.MethodTearDown(cancellationToken);

            if (ShouldCallGlobalTeardown(testProviderIndex, totalTestProviderCount - 1, currentVariableSetIndex, totalNumVariableSets))
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
                continueIterating = instanceContainerEnumerator.MoveNext();
            }
            catch
            {
                await DisposeOfTestInstance(instanceContainerEnumerator.Current);
                throw;
            }

            callback?.Invoke(executionResult);
            results.Add(executionResult);
        } while (continueIterating);

        instanceContainerEnumerator.Dispose();

        return results;
    }

    private async Task<TestExecutionResult> IterateOverVariableCombos(TestInstanceContainer testInstanceContainer, CancellationToken cancellationToken = default)
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

    private static bool ShouldCallGlobalSetup(int testProviderIndex, int currentTestProviderIndex)
    {
        return testProviderIndex == 0 && currentTestProviderIndex == 0;
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
}