using Microsoft.Extensions.DependencyInjection;

namespace Sailfish.Execution;

/// <summary>
///     The result of activating a single test-case instance: the instance itself plus the per-case
///     DI scope it was resolved from (if any).
/// </summary>
/// <remarks>
///     Each test case (method × variable combination) is resolved from its own
///     <see cref="IServiceScope" />. Scoped/transient dependencies are therefore fresh per case and are
///     disposed when the scope is disposed; singletons (a shared server, an <c>ISailfishFixture&lt;T&gt;</c>,
///     anything registered <c>AddSingleton</c>) are resolved from the root container and shared across every
///     case. The engine owns the scope's lifetime and disposes it after the case completes.
///     <para>
///         <see cref="Scope" /> is <c>null</c> for disabled tests, which are never executed and so are not
///         resolved through the container.
///     </para>
/// </remarks>
public sealed class TestInstanceActivation
{
    public TestInstanceActivation(object instance, IServiceScope? scope)
    {
        Instance = instance;
        Scope = scope;
    }

    public object Instance { get; }

    public IServiceScope? Scope { get; }
}
