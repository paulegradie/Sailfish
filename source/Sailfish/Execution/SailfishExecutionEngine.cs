using MediatR;
using Sailfish.Attributes;
using Sailfish.Contracts.Private.ExecutionCallbackHandlers;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using Sailfish.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace Sailfish.Execution;

internal interface ISailfishExecutionEngine
{
    Task<List<TestCaseExecutionResult>> ActivateContainer(
        int testProviderIndex,
        int totalTestProviderCount,
        TestInstanceContainerProvider testProvider,
        MemoryCache memoryCache,
        string providerPropertiesCacheKey,
        CancellationToken cancellationToken = default);

    Task<List<TestCaseExecutionResult>> ActivateContainer(
        int testProviderIndex,
        int totalTestProviderCount,
        TestInstanceContainerProvider testProvider,
        MemoryCache memoryCache,
        string providerPropertiesCacheKey,
        IEnumerable<dynamic> testCaseGroup,
        CancellationToken cancellationToken = default);


}

internal class SailfishExecutionEngine(
    ILogger logger,
    IConsoleWriter consoleWriter,
    ITestCaseIterator testCaseIterator,
    ITestCaseCountPrinter testCaseCountPrinter,
    IMediator mediator,
    IClassExecutionSummaryCompiler classExecutionSummaryCompiler,
    IRunSettings runSettings) : ISailfishExecutionEngine
{
    private readonly IClassExecutionSummaryCompiler classExecutionSummaryCompiler = classExecutionSummaryCompiler;
    private readonly ILogger logger = logger;
    private readonly IConsoleWriter consoleWriter = consoleWriter;
    private readonly IMediator mediator = mediator;
    private readonly IRunSettings runSettings = runSettings;
    private readonly ITestCaseCountPrinter testCaseCountPrinter = testCaseCountPrinter;
    private readonly ITestCaseIterator testCaseIterator = testCaseIterator;

    public async Task<List<TestCaseExecutionResult>> ActivateContainer(
        [Range(1, int.MaxValue)] int testProviderIndex,
        [Range(1, int.MaxValue)] int totalTestProviderCount,
        TestInstanceContainerProvider testProvider,
        MemoryCache memoryCache,
        string providerPropertiesCacheKey,
        CancellationToken cancellationToken = default) 
        => await ActivateContainer(
            testProviderIndex, 
            totalTestProviderCount, 
            testProvider, 
            memoryCache, 
            providerPropertiesCacheKey, 
            new List<object>(), 
            cancellationToken);

    /// <summary>
    ///     This method is the main entry point for execution in both the main library, as well as the test adapter
    ///     A test instance container is basically a single SailfishMethod from a Sailfish test type. The property
    ///     matrix is provided to each test instance container, and each container will produce as many instances
    ///     as are specified by the property tensor
    /// </summary>
    /// <param name="testProviderIndex"></param>
    /// <param name="totalTestProviderCount"></param>
    /// <param name="testProvider"></param>
    /// <param name="memoryCache"></param>
    /// <param name="providerPropertiesCacheKey"></param>
    /// <param name="preTestCallback"></param>
    /// <param name="postTestCallback"></param>
    /// <param name="exceptionCallback"></param>
    /// <param name="testDisabledCallback"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<TestCaseExecutionResult>> ActivateContainer(
        [Range(1, int.MaxValue)] int testProviderIndex,
        [Range(1, int.MaxValue)] int totalTestProviderCount,
        TestInstanceContainerProvider testProvider,
        MemoryCache memoryCache,
        string providerPropertiesCacheKey,
        IEnumerable<dynamic> testCaseGroup,
        CancellationToken cancellationToken = default)
    {
        if (testProviderIndex > totalTestProviderCount)
            throw new SailfishException($"The test provider index {testProviderIndex} cannot be greater than total test provider count {totalTestProviderCount}");

        var currentPropertyTensorIndex = 0;
        var totalPropertyTensorElements = Math.Max(testProvider.GetNumberOfPropertySetsInTheQueue() - 1, 0);
        var testCaseEnumerator = testProvider.ProvideNextTestCaseEnumeratorForClass().GetEnumerator();

        try
        {
            testCaseEnumerator.MoveNext();
        }
        catch (Exception ex)
        {
            await mediator.Publish(new ExceptionNotification(testCaseEnumerator.Current, testCaseGroup), cancellationToken);
            await DisposeOfTestInstance(testCaseEnumerator.Current);
            testCaseEnumerator.Dispose();
            var msg = $"Error resolving test from {testProvider.Test.FullName}";
            logger.Log(LogLevel.Fatal, ex, msg);

            var testCaseEnumerationException = new TestCaseEnumerationException(ex, $"Failed to create test cases for {testProvider.Test.FullName}");
            return [new(testCaseEnumerationException)];
        }

        var results = new List<TestCaseExecutionResult>();

        if (testProvider.IsDisabled())
        {
            await mediator.Publish(new ExecutionDisabledNotification(testCaseEnumerator.Current, testCaseGroup, true), cancellationToken);   
            testCaseEnumerator.Dispose();
            return results;
        }

        do
        {
            var testCase = testCaseEnumerator.Current;
            try
            {
                if (!testCase.Disabled && memoryCache.Contains(providerPropertiesCacheKey))
                {
                    var savedState = memoryCache.Get(providerPropertiesCacheKey) as PropertiesAndFields;
                    savedState?.ApplyPropertiesAndFieldsTo(testCase.Instance);
                }

                await mediator.Publish(new ExecutionStartingNotification(testCase, testCaseGroup), cancellationToken);
                testCaseCountPrinter.PrintCaseUpdate(testCase.TestCaseId.DisplayName);

                if (ShouldCallGlobalSetup(testProviderIndex, currentPropertyTensorIndex))
                    try
                    {
                        await testCase.CoreInvoker.GlobalSetup(cancellationToken);
                        if (!testCase.Disabled) memoryCache.Add(new CacheItem(providerPropertiesCacheKey, testCase.Instance.RetrievePropertiesAndFields()), new CacheItemPolicy());
                    }
                    catch (Exception ex)
                    {
                        return CatchAndReturn(ex);
                    }

                try
                {
                    await testCase.CoreInvoker.MethodSetup(cancellationToken);
                }
                catch (Exception ex)
                {
                    return CatchAndReturn(ex);
                }

                if (testCase.Disabled)
                {
                    await mediator.Publish(new ExecutionDisabledNotification(testCase, testCaseGroup, false), cancellationToken);

                    currentPropertyTensorIndex += 1;
                    await TryMoveNextOrThrow(testCaseEnumerator, testCaseGroup, cancellationToken);
                    continue;
                }

                var executionResult = await IterateOverVariableCombos(testCase, testCaseGroup, cancellationToken);

                if (!executionResult.IsSuccess) return [executionResult];

                try
                {
                    await testCase.CoreInvoker.MethodTearDown(cancellationToken);
                }
                catch (Exception ex)
                {
                    return CatchAndReturn(ex);
                }

                if (ShouldCallGlobalTeardown(testProviderIndex, totalTestProviderCount, currentPropertyTensorIndex, totalPropertyTensorElements))
                    try
                    {
                        await testCase.CoreInvoker.GlobalTeardown(cancellationToken);
                        if (!testCase.Disabled) memoryCache.Remove(providerPropertiesCacheKey);
                    }
                    catch (Exception ex)
                    {
                        return CatchAndReturn(ex);
                    }

                await mediator.Publish(new ExecutionCompletedNotification(executionResult, testCase, testCaseGroup), cancellationToken);

                results.Add(executionResult);

                if (ShouldDisposeOfInstance(currentPropertyTensorIndex, totalPropertyTensorElements)) await DisposeOfTestInstance(testCase);

                currentPropertyTensorIndex += 1;
            }
            catch (Exception ex)
            {
                var errorResult = new TestCaseExecutionResult(testCase, ex);
                results.Add(errorResult);
            }
        } while (await TryMoveNextOrThrow(testCaseEnumerator, testCaseGroup, cancellationToken));

        testCaseEnumerator.Dispose();

        return results;
    }

    private async Task<bool> TryMoveNextOrThrow(IEnumerator<TestInstanceContainer> instanceContainerEnumerator, IEnumerable<dynamic> testCaseGroup, CancellationToken ct)
    {
        bool continueIterating;
        try
        {
            continueIterating = instanceContainerEnumerator.MoveNext();
        }
        catch (Exception ex)
        {
            await mediator.Publish(new ExceptionNotification(instanceContainerEnumerator.Current, testCaseGroup), ct);
            await DisposeOfTestInstance(instanceContainerEnumerator.Current);
            continueIterating = true;
            consoleWriter.WriteString(ex.Message);
        }

        return continueIterating;
    }

    private static List<TestCaseExecutionResult> CatchAndReturn(Exception ex)
    {
        return new List<TestCaseExecutionResult> { new(ex) };
    }

    private async Task<TestCaseExecutionResult> IterateOverVariableCombos(TestInstanceContainer testInstanceContainer, IEnumerable<dynamic> testCaseGroup, CancellationToken cancellationToken = default)
    {
        try
        {
            var testExecutionResult = await testCaseIterator.Iterate(
                testInstanceContainer,
                runSettings.DisableOverheadEstimation || ShouldDisableOverheadEstimationFromTypeOrMethod(testInstanceContainer),
                cancellationToken).ConfigureAwait(false);

            await mediator.Publish(new TestCaseCompletedNotification(MapResultToTrackingFormat(testInstanceContainer.Type, testExecutionResult)), cancellationToken);

            return testExecutionResult;
        }
        catch (Exception exception)
        {
            if (exception is TestDisabledException) throw;
            await mediator.Publish(new ExceptionNotification(testInstanceContainer, testCaseGroup), cancellationToken);
            return new TestCaseExecutionResult(testInstanceContainer, exception.InnerException ?? exception);
        }
    }

    private ClassExecutionSummaryTrackingFormat MapResultToTrackingFormat(Type testClass, TestCaseExecutionResult testExecutionResult)
    {
        var group = new TestClassResultGroup(testClass, new List<TestCaseExecutionResult> { testExecutionResult });
        return classExecutionSummaryCompiler.CompileToSummaries(new[] { group }).ToTrackingFormat().Single();
    }

    private static bool ShouldDisableOverheadEstimationFromTypeOrMethod(TestInstanceContainer testInstanceContainer)
    {
        var methodDisabled = testInstanceContainer.ExecutionMethod.GetCustomAttribute<SailfishMethodAttribute>()?.DisableOverheadEstimation ?? false;
        var classDisabled = testInstanceContainer.Type.GetCustomAttribute<SailfishAttribute>()?.DisableOverheadEstimation ?? false;
        return methodDisabled || classDisabled;
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
                    if (instanceContainer is not null) instanceContainer.Instance = null!;

                    break;
                }
        }
    }
}