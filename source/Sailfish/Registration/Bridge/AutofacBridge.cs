using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Autofac.Core.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Sailfish.Registration.Bridge;

/// <summary>
///     Transitional compatibility shim that lets legacy Autofac-typed registration callbacks
///     (<see cref="IProvideARegistrationCallback" /> and <see cref="IProvideAdditionalRegistrations" />, both
///     marked <see cref="ObsoleteAttribute"/>) coexist with the new
///     <see cref="IRegisterSailfishServices" /> pipeline.
/// </summary>
/// <remarks>
///     <para>
///         Strategy: build a temporary Autofac <see cref="IContainer"/> from the legacy callbacks, then for each
///         registered service emit a passthrough descriptor onto the supplied <see cref="IServiceCollection"/>
///         that resolves from the Autofac container at request time. The Autofac container's lifetime is bound
///         to a <see cref="BridgeOwner"/> singleton that is registered into the service collection and disposed
///         along with the MS DI <see cref="ServiceProvider"/>.
///     </para>
///     <para>
///         This is intentionally lossless rather than translating-by-rewriting — Autofac registrations can use
///         decorators, registration sources, property injection, named/keyed selectors, and a handful of other
///         features that have no clean MS DI equivalent. Resolving through the live Autofac container preserves
///         all of that, at the cost of keeping Autofac on the runtime classpath through the deprecation window.
///     </para>
///     <para>
///         All of this code is scheduled for deletion once the legacy interfaces are removed in a future major
///         release.
///     </para>
/// </remarks>
internal static class AutofacBridge
{
#pragma warning disable CS0618 // accepting legacy obsolete types is the entire point of this file
    public static async Task RunLegacyCallbacksAsync(
        IServiceCollection services,
        IReadOnlyList<IProvideARegistrationCallback> asyncCallbacks,
        IReadOnlyList<IProvideAdditionalRegistrations> moduleCallbacks,
        CancellationToken cancellationToken)
    {
        if (asyncCallbacks.Count == 0 && moduleCallbacks.Count == 0)
        {
            return;
        }

        var builder = new ContainerBuilder();

        foreach (var module in moduleCallbacks)
        {
            module.Load(builder);
        }

        foreach (var asyncCallback in asyncCallbacks)
        {
            await asyncCallback.RegisterAsync(builder, cancellationToken).ConfigureAwait(false);
        }

        IContainer autofacContainer;
        try
        {
            autofacContainer = builder.Build();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "An error occurred while building the temporary Autofac container used to bridge legacy " +
                "registration callbacks into MS DI. Migrate your IProvideARegistrationCallback / " +
                "IProvideAdditionalRegistrations implementations to IRegisterSailfishServices to fix this " +
                "permanently.", ex);
        }

        var owner = new BridgeOwner(autofacContainer);
        // Singleton so MS DI disposes the Autofac container along with the ServiceProvider.
        services.AddSingleton(owner);

        BridgeRegistrationsIntoServiceCollection(services, autofacContainer);
    }
#pragma warning restore CS0618

    /// <summary>
    ///     Convenience overload used by the legacy <c>SailfishRunner.Run(…, Action&lt;ContainerBuilder&gt;, …)</c>
    ///     overload — runs the supplied builder action against a temporary Autofac container and bridges the
    ///     resulting registrations onto <paramref name="services" />.
    /// </summary>
    public static void BridgeBuilderAction(IServiceCollection services, Action<ContainerBuilder> action)
    {
        var builder = new ContainerBuilder();
        action(builder);
        var container = builder.Build();

        var owner = new BridgeOwner(container);
        services.AddSingleton(owner);
        BridgeRegistrationsIntoServiceCollection(services, container);
    }

    private static void BridgeRegistrationsIntoServiceCollection(IServiceCollection services, IContainer container)
    {
        // Build a (serviceType, IComponentRegistration) map. A single Autofac IComponentRegistration can
        // expose multiple services (e.g. RegisterType<X>().As<IA>().As<IB>()) — we add one MS DI descriptor
        // per service. We treat the lifetime as singleton if it's RootScopeLifetime, transient otherwise —
        // Autofac's InstancePerLifetimeScope and other scope semantics don't map 1:1, and we err on the side
        // of transient (the legacy "InstancePerDependency" default).
        foreach (var registration in container.ComponentRegistry.Registrations)
        {
            var lifetime = registration.Lifetime is RootScopeLifetime
                ? ServiceLifetime.Singleton
                : ServiceLifetime.Transient;

            foreach (var service in registration.Services.OfType<TypedService>())
            {
                var serviceType = service.ServiceType;

                // Skip the marker / housekeeping services Autofac inserts for itself.
                if (serviceType.FullName?.StartsWith("Autofac.", StringComparison.Ordinal) == true)
                {
                    continue;
                }

                services.Add(new ServiceDescriptor(
                    serviceType,
                    sp => sp.GetRequiredService<BridgeOwner>().Container.Resolve(serviceType),
                    lifetime));
            }
        }
    }

    /// <summary>
    ///     Holds the temporary Autofac container alive for as long as the MS DI <see cref="ServiceProvider"/>
    ///     lives. Disposal of the service provider releases this singleton, which in turn disposes the Autofac
    ///     container — no resource leak across the bridge.
    /// </summary>
    [SuppressMessage("Design", "CA1063", Justification = "Simple owner wrapper; full IDisposable pattern is overkill.")]
    internal sealed class BridgeOwner : IDisposable
    {
        public BridgeOwner(IContainer container)
        {
            Container = container;
        }

        public IContainer Container { get; }

        public void Dispose()
        {
            Container.Dispose();
        }
    }
}
