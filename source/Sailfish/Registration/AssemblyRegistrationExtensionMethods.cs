using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Contracts.Public.Models;
using Sailfish.Registration.Bridge;

namespace Sailfish.Registration;

public static class AssemblyRegistrationExtensionMethods
{
    /// <summary>
    ///     Register every service Sailfish needs onto <paramref name="services" />. This is the recommended
    ///     entry point for adding Sailfish to an existing MS DI container.
    /// </summary>
    public static IServiceCollection AddSailfish(this IServiceCollection services, IRunSettings runSettings)
    {
        return services.AddSailfishCore(runSettings);
    }

    /// <summary>
    ///     Legacy Autofac-typed registration entry point. Prefer
    ///     <see cref="AddSailfish(IServiceCollection, IRunSettings)" /> for new code.
    /// </summary>
    /// <remarks>
    ///     Internally this delegates to the same MS DI pipeline used by <see cref="AddSailfish"/> by spinning
    ///     up a transient <see cref="ServiceCollection" />, registering Sailfish services, then bridging them
    ///     back onto the supplied <see cref="ContainerBuilder" />. Provided strictly for backward compatibility.
    /// </remarks>
    [Obsolete("Use IServiceCollection.AddSailfish(runSettings) instead. This Autofac-typed overload is bridged into MS DI for backward compatibility and will be removed in a future major release.", error: false)]
    public static void RegisterSailfishTypes(this ContainerBuilder builder, IRunSettings runSettings)
    {
        // The legacy semantics were "core Sailfish registrations land on this Autofac builder". The post-Phase-A
        // pipeline runs everything through MS DI, so we forward to the new path via an in-process bridge:
        //   1. Build an IServiceCollection populated with the core registrations.
        //   2. Build a ServiceProvider from it.
        //   3. Add Autofac registrations that resolve each service back out of that ServiceProvider.
        // This is the inverse of AutofacBridge and is intentionally simple — the only known caller in this
        // shape is SailfishExecutionCaller's [Obsolete] runner overload, which is itself being deprecated.
        var services = new ServiceCollection();
        services.AddSailfishCore(runSettings);
        var provider = services.BuildServiceProvider();

        // Keep the provider alive for the lifetime of the Autofac container.
        builder.RegisterInstance(provider).As<IServiceProvider>().SingleInstance();
        builder.RegisterBuildCallback(c => c.Resolve<IServiceProvider>()); // ensures provider is rooted

        // Mirror each service descriptor into the Autofac container as a passthrough delegate.
        foreach (var descriptor in services)
        {
            var serviceType = descriptor.ServiceType;
            var registration = builder.Register(_ => provider.GetRequiredService(serviceType)).As(serviceType);
            switch (descriptor.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    registration.SingleInstance();
                    break;
                case ServiceLifetime.Scoped:
                    registration.InstancePerLifetimeScope();
                    break;
                default:
                    registration.InstancePerDependency();
                    break;
            }
        }
    }

    /// <summary>
    ///     Internal Autofac-typed helper retained for the Sailfish.TestAdapter back-compat path while the
    ///     adapter is still being migrated to the new MS DI surface.
    /// </summary>
#pragma warning disable CS0618 // referencing IProvideAdditionalRegistrations is the entire point of this overload
    [Obsolete("Internal back-compat path; use IServiceCollection.AddSailfish + AddSailfishTestAdapter instead.", error: false)]
    internal static void RegisterSailfishTypes(
        this ContainerBuilder builder,
        IRunSettings runSettings,
        params IProvideAdditionalRegistrations[] additionalModules)
    {
        builder.RegisterSailfishTypes(runSettings);
        foreach (var additionalModule in additionalModules) additionalModule.Load(builder);
    }
#pragma warning restore CS0618

    /// <summary>
    ///     Convenience helper that creates a fresh <see cref="ServiceCollection"/>, runs Sailfish's core
    ///     registrations, applies the caller's customisation, then builds an <see cref="IServiceProvider"/>.
    /// </summary>
    internal static IServiceProvider BuildSailfishServiceProvider(
        IRunSettings runSettings,
        Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSailfishCore(runSettings);
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }
}
