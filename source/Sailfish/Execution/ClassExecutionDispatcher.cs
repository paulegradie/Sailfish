using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Contracts.Public.Notifications;

namespace Sailfish.Execution;

/// <summary>
///     Runs all providers (every <c>[SailfishMethod]</c>) of a single test class, applying the class's
///     <see cref="SailfishLifetime" />. Shared by the main-library executor and the test-adapter engine so the
///     instance-lifecycle policy lives in exactly one place.
///     <para>
///         For <see cref="SailfishLifetime.SharedInstance" /> this is also the single owner of the class-level
///         lifecycle: it creates the one instance, runs <c>[SailfishGlobalSetup]</c> once before any case (aborting
///         the class if it throws) and <c>[SailfishGlobalTeardown]</c> once after all cases, and disposes the
///         instance. Every failure path publishes a <see cref="TestCaseExceptionNotification" /> so the test adapter
///         reports it (the adapter records pass/fail solely from that notification) instead of failing silently.
///     </para>
/// </summary>
internal interface IClassExecutionDispatcher
{
    Task<List<TestCaseExecutionResult>> Dispatch(
        Type testType,
        IReadOnlyList<TestInstanceContainerProvider> providers,
        List<dynamic> testCaseGroup,
        CancellationToken cancellationToken = default);
}

internal class ClassExecutionDispatcher : IClassExecutionDispatcher
{
    private readonly ISailfishExecutionEngine _engine;
    private readonly IMediator _mediator;
    private readonly ITypeActivator _typeActivator;

    public ClassExecutionDispatcher(ISailfishExecutionEngine engine, ITypeActivator typeActivator, IMediator mediator)
    {
        _engine = engine;
        _typeActivator = typeActivator;
        _mediator = mediator;
    }

    public async Task<List<TestCaseExecutionResult>> Dispatch(
        Type testType,
        IReadOnlyList<TestInstanceContainerProvider> providers,
        List<dynamic> testCaseGroup,
        CancellationToken cancellationToken = default)
    {
        var results = new List<TestCaseExecutionResult>();
        if (providers.Count == 0) return results;

        var attribute = testType.GetCustomAttribute<SailfishAttribute>();
        var lifetime = attribute?.Lifetime ?? SailfishLifetime.SharedInstance;
        var disabled = attribute?.Disabled ?? false;

        // SharedInstance (default): one instance for the whole class. The ctor + [SailfishGlobalSetup] run once
        // here, [SailfishGlobalTeardown] runs once after all providers, and we dispose the instance. A disabled
        // class falls through to the per-case path, where the engine short-circuits each provider.
        if (lifetime == SailfishLifetime.SharedInstance && !disabled)
        {
            TestInstanceActivation sharedInstance;
            try
            {
                sharedInstance = _typeActivator.CreateDehydratedTestInstance(testType, new TestCaseId(testType.Name));
            }
            catch (Exception ex)
            {
                // The class never instantiated (ctor / dependency-resolution failure). Publish so the adapter fails
                // every case in the group (its handler keys off a null container for exactly this whole-class
                // failure) — never silently.
                await _mediator.Publish(new TestCaseExceptionNotification(null, testCaseGroup, ex), cancellationToken);
                return [new TestCaseExecutionResult(ex)];
            }

            try
            {
                // A representative case bound to the shared instance, so class-level GlobalSetup/GlobalTeardown
                // failures carry a real TestCaseId and run on the shared instance. (In SharedInstance mode the
                // provider hands back the shared instance rather than constructing a new one, so this does not
                // re-run the constructor.)
                var representativeCase = providers[0].ProvideNextTestCaseEnumeratorForClass(sharedInstance.Instance).First();

                try
                {
                    await representativeCase.CoreInvoker.GlobalSetup(cancellationToken);
                }
                catch (Exception ex)
                {
                    // Abort the class — do not run the methods against a half-initialized instance — and report it.
                    await _mediator.Publish(new TestCaseExceptionNotification(representativeCase.ToExternal(), testCaseGroup, ex), cancellationToken);
                    return [new TestCaseExecutionResult(representativeCase, ex)];
                }

                foreach (var provider in providers)
                    results.AddRange(await _engine.ActivateContainer(provider, sharedInstance, testCaseGroup, cancellationToken));

                // GlobalTeardown runs once after all cases, but only if every case succeeded — a lifecycle failure
                // aborts the class and skips teardown (matching the per-case path). Attribute a teardown failure to
                // the last executed case so it surfaces in the IDE; keep the recorded result container-less so the
                // (null-perf) entry never collides with that case's successful row in the reports.
                var lastContainer = results.Count > 0 && results.All(r => r.IsSuccess)
                    ? results[^1].TestInstanceContainer
                    : null;
                if (lastContainer is not null)
                {
                    try
                    {
                        await lastContainer.CoreInvoker.GlobalTeardown(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        await _mediator.Publish(new TestCaseExceptionNotification(lastContainer.ToExternal(), testCaseGroup, ex), cancellationToken);
                        results.Add(new TestCaseExecutionResult(ex));
                    }
                }
            }
            finally
            {
                await DisposeActivation(sharedInstance);
            }

            return results;
        }

        // PerCase (or a disabled class): the engine creates a fresh instance per case and owns its lifecycle.
        foreach (var provider in providers)
            results.AddRange(await _engine.ActivateContainer(provider, null, testCaseGroup, cancellationToken));

        return results;
    }

    private static async Task DisposeActivation(TestInstanceActivation activation)
    {
        switch (activation.Instance)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }

        switch (activation.Scope)
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
