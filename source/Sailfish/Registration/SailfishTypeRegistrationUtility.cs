using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Sailfish.Attributes;
using Sailfish.Exceptions;
using Sailfish.Extensions.Methods;

namespace Sailfish.Registration;

internal static class SailfishTypeRegistrationUtility
{
    public static async Task InvokeRegistrationProviderCallbackMain(
        ContainerBuilder containerBuilder,
        IEnumerable<Type> testDiscoveryAnchorTypes,
        IEnumerable<Type> registrationProviderAnchorTypes,
        CancellationToken cancellationToken = default)
    {
        if (registrationProviderAnchorTypes == null) throw new ArgumentNullException(nameof(registrationProviderAnchorTypes));

        var registrationProviderAssembliesTypes = registrationProviderAnchorTypes
            .Select(t => t.Assembly)
            .Distinct()
            .SelectMany(x => x.GetTypes())
            .ToList();
        await RegisterCallbackProviders(containerBuilder, registrationProviderAssembliesTypes, cancellationToken);

        var testAssembliesTypes = testDiscoveryAnchorTypes
            .Select(t => t.Assembly)
            .Distinct()
            .SelectMany(x => x.GetTypes())
            .ToList();

        var allSourcesForOtherRegistrations = new List<Type>();
        allSourcesForOtherRegistrations.AddRange(registrationProviderAssembliesTypes);
        allSourcesForOtherRegistrations.AddRange(testAssembliesTypes);
        RegisterISailfishFixtureGenericTypes(containerBuilder, allSourcesForOtherRegistrations);
        RegisterISailfishDependencyTypes(containerBuilder, allSourcesForOtherRegistrations);

        RegisterAllTestTypes(containerBuilder, testAssembliesTypes);
    }

    private static void RegisterAllTestTypes(ContainerBuilder containerBuilder, IEnumerable<Type> testAssembliesTypes)
    {
        var testTypes = testAssembliesTypes.Distinct().Where(type => type.HasAttribute<SailfishAttribute>());
        containerBuilder.RegisterTypes(testTypes.ToArray()).FindConstructorsWith(new RelaxedConstructorFinder());
    }

    private static void RegisterISailfishDependencyTypes(ContainerBuilder containerBuilder, IEnumerable<Type> types)
    {
        var basicTypes = types.Where(t => t.GetInterfaces().Contains(typeof(ISailfishDependency)));
        foreach (var basicType in basicTypes)
        {
            containerBuilder.RegisterType(basicType).AsSelf();
        }
    }

    private static void RegisterISailfishFixtureGenericTypes(ContainerBuilder containerBuilder, IEnumerable<Type> allAssemblyTypes)
    {
        var typesInsideTheGenericISailfishFixture = allAssemblyTypes.Where(IsISailfishFixture);
        var genericTypeArgs = typesInsideTheGenericISailfishFixture.SelectMany(GetISailfishFixtureGenericArgs).Distinct();
        containerBuilder.RegisterTypes(genericTypeArgs.ToArray());
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
        ContainerBuilder containerBuilder,
        IEnumerable<Type> allAssemblyTypes,
        CancellationToken cancellationToken)
    {
        var assemblyTypes = allAssemblyTypes.ToList();
        var asyncProviders = GetRegistrationCallbackProviders<IProvideARegistrationCallback>(assemblyTypes).ToList();
        if (!asyncProviders.Any())
        {
            asyncProviders = GetRegistrationCallbackProviders<IProvideARegistrationCallback>(assemblyTypes).ToList();
        }

        foreach (var asyncCallback in asyncProviders)
        {
            var methodInfo = asyncCallback.GetType().GetMethod(nameof(IProvideARegistrationCallback.RegisterAsync));
            if (methodInfo is not null && methodInfo.IsAsyncMethod())
            {
                await asyncCallback.RegisterAsync(containerBuilder, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var task = asyncCallback.RegisterAsync(containerBuilder, cancellationToken);
                if (!task.IsCompletedSuccessfully)
                {
                    throw new Exception("Task error in your registration callback");
                }
            }
        }
    }

    public static IEnumerable<T> GetRegistrationCallbackProviders<T>(IEnumerable<Type> allAssemblyTypes)
    {
        var providers = allAssemblyTypes
            .Where(type => type.GetInterfaces().Contains(typeof(T)))
            .Distinct()
            .Select(Activator.CreateInstance)
            .Cast<T>()
            .ToList();

        return providers;
    }
}