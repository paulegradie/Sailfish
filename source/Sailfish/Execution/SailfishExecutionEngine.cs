using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Exceptions;
using Sailfish.Program;
using Serilog;

namespace Sailfish.Execution;

internal class SailfishExecutionEngine : ISailfishExecutionEngine
{
    private readonly ITestCaseIterator testCaseIterator;
    private readonly RunSettings runSettings;

    public SailfishExecutionEngine(ITestCaseIterator testCaseIterator, RunSettings runSettings)
    {
        this.testCaseIterator = testCaseIterator;
        this.runSettings = runSettings;
    }

    /// <summary>
    /// This method is the main entry point for execution in both the main library, as well as the test adapter
    /// A test instance container is basically a single SailfishMethod from a Sailfish test type. The property
    /// matrix is provided to each test instance container, and each container will produce as many instances
    /// as are specified by the property tensor
    /// </summary>
    /// <param name="testProviderIndex"></param>
    /// <param name="totalTestProviderCount"></param>
    /// <param name="testProvider"></param>
    /// <param name="callback"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<TestExecutionResult>> ActivateContainer(
        [Range(1, int.MaxValue)] int testProviderIndex,
        [Range(1, int.MaxValue)] int totalTestProviderCount,
        TestInstanceContainerProvider testProvider,
        Action<TestExecutionResult>? callback = null,
        CancellationToken cancellationToken = default)
    {
        if (testProviderIndex > totalTestProviderCount)
        {
            throw new SailfishException($"test provider index {testProviderIndex} cannot be greater than total test provider count {totalTestProviderCount}");
        }

        var currentPropertyTensorIndex = 1;
        var totalPropertyTensorElements = testProvider.GetNumberOfPropertySetsInTheQueue();

        var instanceContainerEnumerator = testProvider.ProvideNextTestInstanceContainer(runSettings.TestLocationAnchors, runSettings.RegistrationProviderAnchors).GetAsyncEnumerator(cancellationToken);

        try
        {
            await instanceContainerEnumerator.MoveNextAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await DisposeOfTestInstance(instanceContainerEnumerator.Current);
            await instanceContainerEnumerator.DisposeAsync();
            Log.Logger.Fatal(ex, "Error encountered when getting next test");
            throw;
        }

        var results = new List<TestExecutionResult>();
        bool continueIterating;
        do
        {
            var testMethodContainer = instanceContainerEnumerator.Current;
            TestCaseCountPrinter.PrintCaseUpdate(testMethodContainer.TestCaseId.DisplayName);

            if (ShouldCallGlobalSetup(testProviderIndex, currentPropertyTensorIndex))
            {
                await testMethodContainer.Invocation.GlobalSetup(cancellationToken);
            }

            await testMethodContainer.Invocation.MethodSetup(cancellationToken);

            var executionResult = await IterateOverVariableCombos(testMethodContainer, cancellationToken);

            await testMethodContainer.Invocation.MethodTearDown(cancellationToken);

            if (ShouldCallGlobalTeardown(testProviderIndex, totalTestProviderCount, currentPropertyTensorIndex, totalPropertyTensorElements))
            {
                await testMethodContainer.Invocation.GlobalTeardown(cancellationToken);
            }

            if (ShouldDisposeOfInstance(currentPropertyTensorIndex, totalPropertyTensorElements))
            {
                await DisposeOfTestInstance(testMethodContainer);
            }

            currentPropertyTensorIndex += 1;

            try
            {
                continueIterating = await instanceContainerEnumerator.MoveNextAsync(cancellationToken);
            }
            catch
            {
                await DisposeOfTestInstance(instanceContainerEnumerator.Current);
                throw;
            }

            callback?.Invoke(executionResult);
            results.Add(executionResult);
        } while (continueIterating);

        await instanceContainerEnumerator.DisposeAsync();

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