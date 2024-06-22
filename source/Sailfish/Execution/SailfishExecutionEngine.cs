using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;
using Sailfish.Utils;

namespace Sailfish.Execution;

internal interface ISailfishExecutionEngine
{
    Task<List<TestCaseExecutionResult>> ActivateContainer(
        int testProviderIndex,
        int totalTestProviderCount,
        ITestInstanceContainerProvider testProvider,
        MemoryCache memoryCache,
        string providerPropertiesCacheKey,
        CancellationToken cancellationToken = default);

    Task<List<TestCaseExecutionResult>> ActivateContainer(
        int testProviderIndex,
        int totalTestProviderCount,
        ITestInstanceContainerProvider testProvider,
        MemoryCache memoryCache,
        string providerPropertiesCacheKey,
        List<dynamic> testCaseGroup,
        CancellationToken cancellationToken = default);
}

internal class SailfishExecutionEngine : ISailfishExecutionEngine
{
    private readonly IClassExecutionSummaryCompiler classExecutionSummaryCompiler;
    private readonly IConsoleWriter consoleWriter;
    private readonly ILogger logger;
    private readonly IMediator mediator;
    private readonly IRunSettings runSettings;
    private readonly ITestCaseCountPrinter testCaseCountPrinter;
    private readonly ITestCaseIterator testCaseIterator;

    public SailfishExecutionEngine(
        ILogger logger,
        IConsoleWriter consoleWriter,
        ITestCaseIterator testCaseIterator,
        ITestCaseCountPrinter testCaseCountPrinter,
        IMediator mediator,
        IClassExecutionSummaryCompiler classExecutionSummaryCompiler,
        IRunSettings runSettings)
    {
        this.classExecutionSummaryCompiler = classExecutionSummaryCompiler;
        this.logger = logger;
        this.consoleWriter = consoleWriter;
        this.mediator = mediator;
        this.runSettings = runSettings;
        this.testCaseCountPrinter = testCaseCountPrinter;
        this.testCaseIterator = testCaseIterator;
    }

    public async Task<List<TestCaseExecutionResult>> ActivateContainer(
        [Range(1, int.MaxValue)] int testProviderIndex,
        [Range(1, int.MaxValue)] int totalTestProviderCount,
        ITestInstanceContainerProvider testProvider,
        MemoryCache memoryCache,
        string providerPropertiesCacheKey,
        CancellationToken cancellationToken = default)
    {
        return await ActivateContainer(
            testProviderIndex,
            totalTestProviderCount,
            testProvider,
            memoryCache,
            providerPropertiesCacheKey,
            [],
            cancellationToken);
    }

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
    /// <param name="testCaseGroup"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<TestCaseExecutionResult>> ActivateContainer(
        [Range(1, int.MaxValue)] int testProviderIndex,
        [Range(1, int.MaxValue)] int totalTestProviderCount,
        ITestInstanceContainerProvider testProvider,
        MemoryCache memoryCache,
        string providerPropertiesCacheKey,
        List<dynamic> testCaseGroup,
        CancellationToken cancellationToken = default)
    {
        if (testProviderIndex > totalTestProviderCount)
            throw new SailfishException(
                $"The test provider index {testProviderIndex} cannot be greater than total test provider count {totalTestProviderCount}");

        var currentVariableSetIndex = 0;
        var totalNumVariableSets = Math.Max(testProvider.GetNumberOfPropertySetsInTheQueue() - 1, 0);
        var testCaseEnumerator = testProvider.ProvideNextTestCaseEnumeratorForClass().GetEnumerator();

        try
        {
            testCaseEnumerator.MoveNext();
        }
        catch (Exception ex)
        {
            await mediator.Publish(new TestCaseExceptionNotification(testCaseEnumerator.Current.ToExternal(), testCaseGroup, ex),
                cancellationToken);
            await DisposeOfTestInstance(testCaseEnumerator.Current);
            testCaseEnumerator.Dispose();
            var msg = $"Error resolving test from {testProvider.Test.FullName}";
            logger.Log(LogLevel.Fatal, ex, msg);

            var testCaseEnumerationException = new TestCaseEnumerationException(ex,
                $"Failed to create test cases for {testProvider.Test.FullName}");
            return [new TestCaseExecutionResult(testCaseEnumerationException)];
        }

        var results = new List<TestCaseExecutionResult>();

        if (testProvider.IsDisabled())
        {
            await mediator.Publish(
                new TestCaseDisabledNotification(testCaseEnumerator.Current.ToExternal(), testCaseGroup.ToList(), true),
                cancellationToken);
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

                await mediator.Publish(new TestCaseStartedNotification(testCase.ToExternal(), testCaseGroup), cancellationToken);
                testCaseCountPrinter.PrintCaseUpdate(testCase.TestCaseId.DisplayName);

                if (ShouldCallGlobalSetup(testProviderIndex, currentVariableSetIndex))
                    try
                    {
                        await testCase.CoreInvoker.GlobalSetup(cancellationToken);
                        if (!testCase.Disabled)
                            memoryCache.Add(
                                new CacheItem(providerPropertiesCacheKey,
                                    testCase.Instance.RetrievePropertiesAndFields()), new CacheItemPolicy());
                    }
                    catch (Exception ex)
                    {
                        return await CatchAndReturn(ex, testCase, testCaseGroup, cancellationToken);
                    }

                if (testCase.Disabled)
                {
                    await mediator.Publish(
                        new TestCaseDisabledNotification(testCase.ToExternal(), testCaseGroup, false),
                        cancellationToken);

                    currentVariableSetIndex += 1;
                    await TryMoveNextOrThrow(testCaseEnumerator, testCaseGroup, cancellationToken);
                    continue;
                }

                try
                {
                    await testCase.CoreInvoker.MethodSetup(cancellationToken);
                }
                catch (Exception ex)
                {
                    return await CatchAndReturn(ex, testCase, testCaseGroup, cancellationToken);
                }

                var executionResult = await IterateOverVariableCombos(testCase, testCaseGroup, cancellationToken);

                // TODO: Allow users to force method teardown on failure
                if (!executionResult.IsSuccess) return [executionResult];

                try
                {
                    await testCase.CoreInvoker.MethodTearDown(cancellationToken);
                }
                catch (Exception ex)
                {
                    return await CatchAndReturn(ex, testCase, testCaseGroup, cancellationToken);
                }

                if (ShouldCallGlobalTeardown(testProviderIndex, totalTestProviderCount, currentVariableSetIndex,
                        totalNumVariableSets))
                    try
                    {
                        await testCase.CoreInvoker.GlobalTeardown(cancellationToken);
                        if (!testCase.Disabled) memoryCache.Remove(providerPropertiesCacheKey);
                    }
                    catch (Exception ex)
                    {
                        return await CatchAndReturn(ex, testCase, testCaseGroup, cancellationToken);
                    }

                var testCaseSummary = classExecutionSummaryCompiler.CompileToSummaries([
                    new TestClassResultGroup(
                        executionResult.TestInstanceContainer!.Type,
                        [executionResult])
                ]);
                await mediator.Publish(new TestClassCompletedNotification(testCaseSummary.ToTrackingFormat().Single(), testCase.ToExternal(), testCaseGroup), cancellationToken);

                results.Add(executionResult);

                if (ShouldDisposeOfInstance(currentVariableSetIndex, totalNumVariableSets)) await DisposeOfTestInstance(testCase);

                currentVariableSetIndex += 1;
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

    private async Task<bool> TryMoveNextOrThrow(
        IEnumerator<TestInstanceContainer> instanceContainerEnumerator,
        IEnumerable<dynamic> testCaseGroup,
        CancellationToken ct)
    {
        bool continueIterating;
        try
        {
            continueIterating = instanceContainerEnumerator.MoveNext();
        }
        catch (Exception ex)
        {
            await mediator.Publish(new TestCaseExceptionNotification(instanceContainerEnumerator.Current.ToExternal(), testCaseGroup, ex), ct);
            await DisposeOfTestInstance(instanceContainerEnumerator.Current);
            continueIterating = true;
            consoleWriter.WriteString(ex.Message);
        }

        return continueIterating;
    }

    private async Task<List<TestCaseExecutionResult>> CatchAndReturn(Exception ex, TestInstanceContainer testCase,
        IEnumerable<dynamic> testCaseGroup, CancellationToken cancellationToken)
    {
        if (ex is NullReferenceException)
            ex = new NullReferenceException(ex.Message + Environment.NewLine + $"Null variable or property encountered in method: {testCase.ExecutionMethod.Name}");

        await mediator.Publish(new TestCaseExceptionNotification(testCase.ToExternal(), testCaseGroup, ex), cancellationToken);
        return [new TestCaseExecutionResult(testCase, ex)];
    }

    private async Task<TestCaseExecutionResult> IterateOverVariableCombos(
        TestInstanceContainer testInstanceContainer,
        IEnumerable<dynamic> testCaseGroup,
        CancellationToken cancellationToken = default)
    {
        TestCaseExecutionResult testCaseExecutionResult;
        try // this is not great control flow - this is where we catch SailfishMethod exceptions
        {
            testCaseExecutionResult = await testCaseIterator.Iterate(
                testInstanceContainer,
                runSettings.DisableOverheadEstimation ||
                ShouldDisableOverheadEstimationFromTypeOrMethod(testInstanceContainer),
                cancellationToken);

            if (!testCaseExecutionResult.IsSuccess)
            {
                await mediator.Publish(new TestCaseExceptionNotification(testInstanceContainer.ToExternal(), testCaseGroup, testCaseExecutionResult.Exception), cancellationToken);
                return new TestCaseExecutionResult(testInstanceContainer,
                    testCaseExecutionResult.Exception!.InnerException ?? testCaseExecutionResult.Exception);
            }
        }
        catch (Exception ex)
        {
            testCaseExecutionResult = new TestCaseExecutionResult(testInstanceContainer, ex);
        }

        await mediator.Publish(
            new TestCaseCompletedNotification(
                MapResultToTrackingFormat(testInstanceContainer.Type, testCaseExecutionResult),
                testInstanceContainer.ToExternal(),
                testCaseGroup
            ), cancellationToken);


        return testCaseExecutionResult;
    }

    private ClassExecutionSummaryTrackingFormat MapResultToTrackingFormat(
        Type testClass,
        TestCaseExecutionResult testExecutionResult)
    {
        var group = new TestClassResultGroup(testClass, [testExecutionResult]);
        return classExecutionSummaryCompiler.CompileToSummaries(new[] { group }).ToTrackingFormat().Single();
    }

    private static bool ShouldDisableOverheadEstimationFromTypeOrMethod(TestInstanceContainer testInstanceContainer)
    {
        var methodDisabled = testInstanceContainer.ExecutionMethod.GetCustomAttribute<SailfishMethodAttribute>()?.DisableOverheadEstimation ?? false;
        var classDisabled = testInstanceContainer.Type.GetCustomAttribute<SailfishAttribute>()?.DisableOverheadEstimation ?? false;
        return methodDisabled || classDisabled;
    }

    private static bool ShouldCallGlobalTeardown(int methodIndex, int totalMethodCount, int currentVariableSetIndex,
        int totalNumVariableSets)
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