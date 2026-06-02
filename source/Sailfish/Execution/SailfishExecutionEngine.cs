using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;
using Sailfish.Contracts.Public.Serialization.Tracking.V1;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;
using Sailfish.Logging;
using Sailfish.Presentation;
using Sailfish.Presentation.Console;

namespace Sailfish.Execution;

internal interface ISailfishExecutionEngine
{
    Task<List<TestCaseExecutionResult>> ActivateContainer(
        ITestInstanceContainerProvider testProvider,
        CancellationToken cancellationToken = default);

    Task<List<TestCaseExecutionResult>> ActivateContainer(
        ITestInstanceContainerProvider testProvider,
        List<dynamic> testCaseGroup,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Runs one provider (one <c>[SailfishMethod]</c>) for a class.
    /// </summary>
    /// <param name="providerIndex">0-based index of this provider among the class's providers.</param>
    /// <param name="providerCount">Total number of providers for the class.</param>
    /// <param name="sharedInstance">
    ///     Non-null for <see cref="SailfishLifetime.SharedInstance" />: the one class-level instance every case
    ///     reuses. GlobalSetup runs once on the first provider, GlobalTeardown once on the last provider; the engine
    ///     does not create or dispose instances. Null for <see cref="SailfishLifetime.PerCase" />: a fresh instance
    ///     per case, with GlobalSetup/GlobalTeardown and disposal per case. The caller owns the shared instance's
    ///     lifetime (creation and disposal).
    /// </param>
    Task<List<TestCaseExecutionResult>> ActivateContainer(
        int providerIndex,
        int providerCount,
        ITestInstanceContainerProvider testProvider,
        TestInstanceActivation? sharedInstance,
        List<dynamic> testCaseGroup,
        CancellationToken cancellationToken = default);
}

internal class SailfishExecutionEngine : ISailfishExecutionEngine
{
    private readonly IClassExecutionSummaryCompiler _classExecutionSummaryCompiler;
    private readonly IConsoleWriter _consoleWriter;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IRunSettings _runSettings;
    private readonly ITestCaseCountPrinter _testCaseCountPrinter;
    private readonly ITestCaseIterator _testCaseIterator;

    public SailfishExecutionEngine(
        ILogger logger,
        IConsoleWriter consoleWriter,
        ITestCaseIterator testCaseIterator,
        ITestCaseCountPrinter testCaseCountPrinter,
        IMediator mediator,
        IClassExecutionSummaryCompiler classExecutionSummaryCompiler,
        IRunSettings runSettings)
    {
        _classExecutionSummaryCompiler = classExecutionSummaryCompiler;
        _logger = logger;
        _consoleWriter = consoleWriter;
        _mediator = mediator;
        _runSettings = runSettings;
        _testCaseCountPrinter = testCaseCountPrinter;
        _testCaseIterator = testCaseIterator;
    }

    public Task<List<TestCaseExecutionResult>> ActivateContainer(
        ITestInstanceContainerProvider testProvider,
        CancellationToken cancellationToken = default)
        => ActivateContainer(0, 1, testProvider, null, [], cancellationToken);

    public Task<List<TestCaseExecutionResult>> ActivateContainer(
        ITestInstanceContainerProvider testProvider,
        List<dynamic> testCaseGroup,
        CancellationToken cancellationToken = default)
        => ActivateContainer(0, 1, testProvider, null, testCaseGroup, cancellationToken);

    public async Task<List<TestCaseExecutionResult>> ActivateContainer(
        int providerIndex,
        int providerCount,
        ITestInstanceContainerProvider testProvider,
        TestInstanceActivation? sharedInstance,
        List<dynamic> testCaseGroup,
        CancellationToken cancellationToken = default)
    {
        var shared = sharedInstance is not null;
        var isFirstProvider = providerIndex <= 0;
        var isLastProvider = providerIndex >= providerCount - 1;

        var testCaseEnumerator = testProvider.ProvideNextTestCaseEnumeratorForClass(sharedInstance?.Instance).GetEnumerator();

        try
        {
            testCaseEnumerator.MoveNext();
        }
        catch (Exception ex)
        {
            var currentInstance = TryGetCurrent(testCaseEnumerator);

            await _mediator.Publish(
                new TestCaseExceptionNotification(currentInstance?.ToExternal(), testCaseGroup, ex),
                cancellationToken);
            if (!shared) await DisposeOfTestInstance(currentInstance);
            testCaseEnumerator.Dispose();
            var msg = $"Error resolving test from {testProvider.Test.FullName}";
            _logger.Log(LogLevel.Fatal, ex, msg);

            if (ex is TestClassInstantiationException)
            {
                return [new TestCaseExecutionResult(ex)];
            }

            var testCaseEnumerationException = new TestCaseEnumerationException(ex,
                $"Failed to create test cases for {testProvider.Test.FullName}");
            return [new TestCaseExecutionResult(testCaseEnumerationException)];
        }

        var results = new List<TestCaseExecutionResult>();

        if (testProvider.IsDisabled())
        {
            await _mediator.Publish(
                new TestCaseDisabledNotification(testCaseEnumerator.Current.ToExternal(), testCaseGroup, true),
                cancellationToken);
            if (!shared) await DisposeOfTestInstance(testCaseEnumerator.Current);
            testCaseEnumerator.Dispose();
            return results;
        }

        // SharedInstance: GlobalSetup runs ONCE for the class (first provider only), on the shared instance.
        // The first case's CoreInvoker is bound to that shared instance; reuse it for GlobalSetup, and keep a
        // reference so GlobalTeardown can run after the loop on the last provider.
        var sharedInvoker = shared ? testCaseEnumerator.Current.CoreInvoker : null;
        if (shared && isFirstProvider)
        {
            try
            {
                await testCaseEnumerator.Current.CoreInvoker.GlobalSetup(cancellationToken);
            }
            catch (Exception ex)
            {
                var failure = await CatchAndReturn(ex, testCaseEnumerator.Current, testCaseGroup, cancellationToken);
                testCaseEnumerator.Dispose();
                return failure;
            }
        }

        do
        {
            var testCase = testCaseEnumerator.Current;
            try
            {
                if (testCase.Disabled)
                {
                    await _mediator.Publish(
                        new TestCaseDisabledNotification(testCase.ToExternal(), testCaseGroup, false),
                        cancellationToken);
                    continue;
                }

                await _mediator.Publish(new TestCaseStartedNotification(testCase.ToExternal(), testCaseGroup), cancellationToken);
                _testCaseCountPrinter.PrintCaseUpdate(testCase.TestCaseId.DisplayName);

                // PerCase: GlobalSetup runs on every fresh instance (after its variables are injected, so
                // variable-derived state is correct per case). SharedInstance: it already ran once above.
                if (!shared)
                {
                    try
                    {
                        await testCase.CoreInvoker.GlobalSetup(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        return await CatchAndReturn(ex, testCase, testCaseGroup, cancellationToken);
                    }
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

                if (!executionResult.IsSuccess) return [executionResult];

                try
                {
                    await testCase.CoreInvoker.MethodTearDown(cancellationToken);
                }
                catch (Exception ex)
                {
                    return await CatchAndReturn(ex, testCase, testCaseGroup, cancellationToken);
                }

                // PerCase: GlobalTeardown per case. SharedInstance: deferred to after the loop (last provider).
                if (!shared)
                {
                    try
                    {
                        await testCase.CoreInvoker.GlobalTeardown(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        return await CatchAndReturn(ex, testCase, testCaseGroup, cancellationToken);
                    }
                }

                var testCaseSummary = _classExecutionSummaryCompiler.CompileToSummaries([
                    new TestClassResultGroup(
                        executionResult.TestInstanceContainer!.Type,
                        [executionResult])
                ]);
                await _mediator.Publish(new TestClassCompletedNotification(testCaseSummary.ToTrackingFormat().Single(), testCase.ToExternal(), testCaseGroup), cancellationToken);

                results.Add(executionResult);
            }
            catch (Exception ex)
            {
                var errorResult = new TestCaseExecutionResult(testCase, ex);
                results.Add(errorResult);
            }
            finally
            {
                // PerCase: dispose each fresh instance + its scope. SharedInstance: the caller owns the shared
                // instance's lifetime, so the engine never disposes it here.
                if (!shared) await DisposeOfTestInstance(testCase);
            }
        } while (await TryMoveNextOrThrow(testCaseEnumerator, testCaseGroup, cancellationToken));

        testCaseEnumerator.Dispose();

        // SharedInstance: GlobalTeardown runs ONCE for the class (last provider only), on the shared instance.
        if (shared && isLastProvider && sharedInvoker is not null)
        {
            try
            {
                await sharedInvoker.GlobalTeardown(cancellationToken);
            }
            catch (Exception ex)
            {
                results.Add(new TestCaseExecutionResult(ex));
            }
        }

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
            var currentInstance = TryGetCurrent(instanceContainerEnumerator);
            await _mediator.Publish(new TestCaseExceptionNotification(currentInstance?.ToExternal(), testCaseGroup, ex), ct);
            continueIterating = true;
            _consoleWriter.WriteString(ex.Message);
        }

        return continueIterating;
    }

    private static TestInstanceContainer? TryGetCurrent(IEnumerator<TestInstanceContainer> enumerator)
    {
        // Per IEnumerator<T>: Current is undefined when MoveNext has thrown or returned false.
        // Reading it without a guard masks the real exception with an NRE.
        try
        {
            return enumerator.Current;
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<TestCaseExecutionResult>> CatchAndReturn(Exception ex, TestInstanceContainer testCase,
        IEnumerable<dynamic> testCaseGroup, CancellationToken cancellationToken)
    {
        if (ex is NullReferenceException)
        {
            ex = new NullReferenceException(ex.Message + Environment.NewLine + $"Null variable or property encountered in method: {testCase.ExecutionMethod.Name}");
        }

        await _mediator.Publish(new TestCaseExceptionNotification(testCase.ToExternal(), testCaseGroup, ex), cancellationToken);
        return [new TestCaseExecutionResult(testCase, ex)];
    }

    private async Task<TestCaseExecutionResult> IterateOverVariableCombos(
        TestInstanceContainer testInstanceContainer,
        IReadOnlyCollection<dynamic> testCaseGroup,
        CancellationToken cancellationToken = default)
    {
        TestCaseExecutionResult testCaseExecutionResult;
        try // this is not great flow control - this is where we catch SailfishMethod exceptions
        {
            testCaseExecutionResult = await _testCaseIterator.Iterate(
                testInstanceContainer,
                _runSettings.DisableOverheadEstimation ||
                ShouldDisableOverheadEstimationFromTypeOrMethod(testInstanceContainer),
                cancellationToken);

            if (!testCaseExecutionResult.IsSuccess)
            {
                await _mediator.Publish(new TestCaseExceptionNotification(testInstanceContainer.ToExternal(), testCaseGroup, testCaseExecutionResult.Exception), cancellationToken);
                return new TestCaseExecutionResult(testInstanceContainer,
                    testCaseExecutionResult.Exception!.InnerException ?? testCaseExecutionResult.Exception);
            }
        }
        catch (Exception ex)
        {
            testCaseExecutionResult = new TestCaseExecutionResult(testInstanceContainer, ex);
        }

        await _mediator.Publish(
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
        return _classExecutionSummaryCompiler
            .CompileToSummaries([group])
            .ToTrackingFormat()
            .Single();
    }

    private static bool ShouldDisableOverheadEstimationFromTypeOrMethod(TestInstanceContainer testInstanceContainer)
    {
        var methodDisabled = testInstanceContainer.ExecutionMethod.GetCustomAttribute<SailfishMethodAttribute>()?.DisableOverheadEstimation ?? false;
        var classDisabled = testInstanceContainer.Type.GetCustomAttribute<SailfishAttribute>()?.DisableOverheadEstimation ?? false;
        return methodDisabled || classDisabled;
    }

    private static async Task DisposeOfTestInstance(TestInstanceContainer? instanceContainer)
    {
        // Dispose the instance first so it can still use its scoped dependencies during teardown, then dispose
        // the per-case DI scope (which disposes any scoped/transient dependencies resolved for this case).
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

        switch (instanceContainer?.ServiceScope)
        {
            case IAsyncDisposable asyncDisposableScope:
                await asyncDisposableScope.DisposeAsync();
                break;

            case IDisposable disposableScope:
                disposableScope.Dispose();
                break;
        }
    }
}
