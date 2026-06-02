using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Execution;

/// <summary>
///     Runs all providers (every <c>[SailfishMethod]</c>) of a single test class, applying the class's
///     <see cref="SailfishLifetime" />. Shared by the main-library executor and the test-adapter engine so the
///     instance-lifecycle policy lives in exactly one place.
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
    private readonly ITypeActivator _typeActivator;

    public ClassExecutionDispatcher(ISailfishExecutionEngine engine, ITypeActivator typeActivator)
    {
        _engine = engine;
        _typeActivator = typeActivator;
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

        // SharedInstance (default): one instance for the whole class. The constructor + GlobalSetup run once
        // (GlobalSetup on the first provider, GlobalTeardown on the last — handled inside the engine). We own the
        // shared instance's lifetime: create it once here, dispose it once when the class is done. A disabled class
        // falls through to the per-case path, where the engine short-circuits each provider.
        if (lifetime == SailfishLifetime.SharedInstance && !disabled)
        {
            TestInstanceActivation sharedInstance;
            try
            {
                sharedInstance = _typeActivator.CreateDehydratedTestInstance(testType, new TestCaseId(testType.Name));
            }
            catch (Exception ex)
            {
                // Constructor / dependency-resolution failure for the shared instance surfaces as a class-level
                // failure (mirrors the per-case path, where the engine reports the instantiation exception).
                return [new TestCaseExecutionResult(ex)];
            }

            try
            {
                for (var i = 0; i < providers.Count; i++)
                    results.AddRange(await _engine.ActivateContainer(i, providers.Count, providers[i], sharedInstance, testCaseGroup, cancellationToken));
            }
            finally
            {
                await DisposeActivation(sharedInstance);
            }

            return results;
        }

        // PerCase (or a disabled class): the engine creates a fresh instance per case and owns its lifecycle.
        for (var i = 0; i < providers.Count; i++)
            results.AddRange(await _engine.ActivateContainer(i, providers.Count, providers[i], null, testCaseGroup, cancellationToken));

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
