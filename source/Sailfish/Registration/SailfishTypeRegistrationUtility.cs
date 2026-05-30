using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sailfish.Attributes;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;
using Sailfish.Registration.Bridge;

namespace Sailfish.Registration;

internal static class SailfishTypeRegistrationUtility
{
    /// <summary>
    ///     Discover registration callbacks across the user's test assemblies and register their services into
    ///     <paramref name="services" />. Both the legacy Autofac-typed
    ///     <see cref="IProvideARegistrationCallback" /> / <see cref="IProvideAdditionalRegistrations" /> and the
    ///     new <see cref="IRegisterSailfishServices" /> are discovered; legacy callbacks are funneled through
    ///     <see cref="AutofacBridge" />.
    /// </summary>
    public static async Task InvokeRegistrationProviderCallbackMain(
        IServiceCollection services,
        IEnumerable<Type> testDiscoveryAnchorTypes,
        IEnumerable<Type> registrationProviderAnchorTypes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registrationProviderAnchorTypes);

        var registrationProviderAssembliesTypes = registrationProviderAnchorTypes
            .Select(t => t.Assembly)
            .Distinct()
            .SelectMany(x => x.GetTypes())
            .ToList();
        await RegisterCallbackProviders(services, registrationProviderAssembliesTypes, cancellationToken).ConfigureAwait(false);

        var testAssembliesTypes = testDiscoveryAnchorTypes
            .Select(t => t.Assembly)
            .Distinct()
            .SelectMany(x => x.GetTypes())
            .ToList();

        var allSourcesForOtherRegistrations = new List<Type>();
        allSourcesForOtherRegistrations.AddRange(registrationProviderAssembliesTypes);
        allSourcesForOtherRegistrations.AddRange(testAssembliesTypes);
        RegisterISailfishFixtureGenericTypes(services, allSourcesForOtherRegistrations);
        RegisterISailfishDependencyTypes(services, allSourcesForOtherRegistrations);

        // Test types are NOT registered into MS DI; TypeActivator instantiates them manually via reflection
        // (preserving the historical behaviour of the now-removed RelaxedConstructorFinder, which allowed
        // non-public constructors and bypassed MS DI's public-ctor-only restriction).
        EnsureTestTypesAreActivatableViaTypeActivator(testAssembliesTypes);
    }

    [SuppressMessage("Performance", "CA1822", Justification = "Documentation placeholder; kept as a hook for future validation.")]
    private static void EnsureTestTypesAreActivatableViaTypeActivator(IEnumerable<Type> testAssembliesTypes)
    {
        // No-op today: TypeActivator does its own reflection and does not require MS DI registration of the
        // test class type. Kept as a named seam so a future maintainer can plug validation in if needed.
        _ = testAssembliesTypes.Distinct().Where(type => type.HasAttribute<SailfishAttribute>()).Count();
    }

    private static void RegisterISailfishDependencyTypes(IServiceCollection services, IEnumerable<Type> types)
    {
        // User dependency types implementing the marker interface ISailfishDependency are added as themselves
        // with a transient lifetime; these are user-controlled types so public ctor is a reasonable contract.
        var basicTypes = types.Where(t => t.GetInterfaces().Contains(typeof(ISailfishDependency)));
        foreach (var basicType in basicTypes)
        {
            services.AddTransient(basicType);
        }
    }

    private static void RegisterISailfishFixtureGenericTypes(IServiceCollection services, IEnumerable<Type> allAssemblyTypes)
    {
        var typesInsideTheGenericISailfishFixture = allAssemblyTypes.Where(IsISailfishFixture);
        var genericTypeArgs = typesInsideTheGenericISailfishFixture.SelectMany(GetISailfishFixtureGenericArgs).Distinct();
        foreach (var genericArgType in genericTypeArgs)
        {
            services.AddTransient(genericArgType);
        }
    }

    private static bool IsISailfishFixture(Type type)
    {
        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISailfishFixture<>));
    }

    private static IEnumerable<Type> GetISailfishFixtureGenericArgs(Type type)
    {
        var typeInterfaces = type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISailfishFixture<>));
        var genericArgTypes = new List<Type>();
        foreach (var interfaceType in typeInterfaces)
        {
            var arg = interfaceType.GetGenericArguments();
            if (arg.Length != 1) throw new SailfishException($"{typeof(ISailfishFixture<>).Name} is only allowed to have a single generic argument");
            genericArgTypes.Add(arg.Single());
        }

        return genericArgTypes;
    }

    private static async Task RegisterCallbackProviders(
        IServiceCollection services,
        IEnumerable<Type> allAssemblyTypes,
        CancellationToken cancellationToken)
    {
        var assemblyTypes = allAssemblyTypes.ToList();

        // New API: IRegisterSailfishServices — register directly into IServiceCollection.
        var modernProviders = GetInstancesOf<IRegisterSailfishServices>(assemblyTypes);
        foreach (var provider in modernProviders)
        {
            await provider.RegisterAsync(services, cancellationToken).ConfigureAwait(false);
        }

        // Legacy API: IProvideARegistrationCallback / IProvideAdditionalRegistrations — funneled through the
        // Autofac compat bridge. The bridge spins up a temporary Autofac ContainerBuilder, lets the legacy
        // callback register into it, then converts each resulting Autofac registration into a passthrough
        // service descriptor on the IServiceCollection.
#pragma warning disable CS0618 // intentionally calling the obsolete legacy interface
        var legacyAsyncProviders = GetInstancesOf<IProvideARegistrationCallback>(assemblyTypes);
        var legacyModuleProviders = GetInstancesOf<IProvideAdditionalRegistrations>(assemblyTypes)
            // Filter out SailfishModuleRegistrations if any external subclass shows up — though there shouldn't
            // be any, since the type is now an internal static class with no public surface.
            .ToList();

        if (legacyAsyncProviders.Count > 0 || legacyModuleProviders.Count > 0)
        {
            await AutofacBridge.RunLegacyCallbacksAsync(
                services,
                legacyAsyncProviders,
                legacyModuleProviders,
                cancellationToken).ConfigureAwait(false);
        }
#pragma warning restore CS0618
    }

    private static List<T> GetInstancesOf<T>(IEnumerable<Type> allAssemblyTypes)
    {
        return allAssemblyTypes
            .Where(type => !type.IsAbstract && !type.IsInterface && type.GetInterfaces().Contains(typeof(T)))
            .Distinct()
            .Select(t =>
            {
                try
                {
                    return (T?)Activator.CreateInstance(t);
                }
                catch (Exception ex)
                {
                    throw new SailfishException(
                        $"Failed to construct an instance of '{t.FullName}' (implements '{typeof(T).Name}'). " +
                        $"Registration provider types must have a public parameterless constructor. Cause: {ex.GetType().Name}: {ex.Message}");
                }
            })
            .Where(x => x is not null)
            .Cast<T>()
            .ToList();
    }
}
