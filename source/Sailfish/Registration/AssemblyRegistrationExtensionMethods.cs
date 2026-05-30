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
    ///     <para>
    ///         Internally this delegates to the same MS DI pipeline used by <see cref="AddSailfish"/> by
    ///         building an <see cref="IServiceCollection"/> populated with Sailfish's core registrations and
    ///         mirroring it onto the supplied <see cref="ContainerBuilder"/> as passthrough delegates. The
    ///         underlying <see cref="IServiceProvider"/> is built lazily on first resolution from a
    ///         <c>RegisterBuildCallback</c> hook, so callers that add their own registrations to the same
    ///         builder *before* calling <see cref="Autofac.ContainerBuilder.Build"/> will see Sailfish's
    ///         services normally.
    ///     </para>
    ///     <para>
    ///         <b>Known limitation:</b> registrations the caller adds to the Autofac builder are <i>not</i>
    ///         visible to Sailfish's MS-DI-resolved internals (e.g. <c>TypeActivator</c>'s constructor
    ///         injection). If your code injects Autofac-only registrations into Sailfish test classes, migrate
    ///         to <see cref="AddSailfish(IServiceCollection, IRunSettings)"/> combined with
    ///         <see cref="IRegisterSailfishServices"/> — those are wired through the same single
    ///         <see cref="IServiceProvider"/> that <c>TypeActivator</c> uses.
    ///     </para>
    /// </remarks>
    [Obsolete("Use IServiceCollection.AddSailfish(runSettings) and IRegisterSailfishServices instead. This Autofac-typed overload is bridged into MS DI for backward compatibility; registrations added directly to the ContainerBuilder are NOT visible to Sailfish's internal resolution pipeline. This overload will be removed in a future major release.", error: false)]
    public static void RegisterSailfishTypes(this ContainerBuilder builder, IRunSettings runSettings)
    {
        var services = new ServiceCollection();
        services.AddSailfishCore(runSettings);

        // Defer building the ServiceProvider until the Autofac container itself is built. This (a) avoids
        // a wasted BuildServiceProvider call if the consumer mutates the IServiceCollection state somehow
        // and (b) keeps the ServiceProvider's lifetime cleanly bound to the Autofac container.
        IServiceProvider? cachedProvider = null;
        IServiceProvider GetProvider() => cachedProvider ??= services.BuildServiceProvider();

        // Mirror each service descriptor into Autofac as a passthrough delegate. We capture the local
        // services collection and resolve through the lazy GetProvider on each Autofac resolve.
        foreach (var descriptor in services)
        {
            var serviceType = descriptor.ServiceType;
            var registration = builder.Register(_ => GetProvider().GetRequiredService(serviceType)).As(serviceType);
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

        // Expose the IServiceProvider on the Autofac side too, for callers that want to resolve Sailfish
        // services without going through Autofac (rare).
        builder.Register(_ => GetProvider()).As<IServiceProvider>().SingleInstance();
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
