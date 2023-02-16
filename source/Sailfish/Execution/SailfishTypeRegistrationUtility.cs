using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Sailfish.AdapterUtils;
using Sailfish.Exceptions;
using Sailfish.ExtensionMethods;
using Sailfish.Registration;

namespace Sailfish.Execution;

internal static class SailfishTypeRegistrationUtility
{
    public static async Task InvokeRegistrationProviderCallbackMain(
        ContainerBuilder containerBuilder,
        IEnumerable<Type> testDiscoveryAnchorTypes,
        Type[] registrationProviderAnchorTypes,
        CancellationToken cancellationToken = default)
    {
        if (registrationProviderAnchorTypes == null) throw new ArgumentNullException(nameof(registrationProviderAnchorTypes));
        var allAssemblies = testDiscoveryAnchorTypes.Select(t => t.Assembly);
        var allAssemblyTypes = allAssemblies.Distinct().SelectMany(a => a.GetTypes()).Distinct().ToArray();

        await RegisterSupportedIdentifierTypes(containerBuilder, allAssemblyTypes, registrationProviderAnchorTypes, cancellationToken);
    }

    public static async Task InvokeRegistrationProviderCallbackAdapter(
        ContainerBuilder containerBuilder,
        Type aTestType,
        CancellationToken cancellationToken = default)
    {
        var allAssemblies = new List<Assembly>() { aTestType.Assembly };
        var allAssemblyTypes = allAssemblies.Distinct().SelectMany(a => a.GetTypes()).Distinct().ToArray();

        await RegisterSupportedIdentifierTypes(containerBuilder, allAssemblyTypes, new List<Type>() { aTestType }, cancellationToken);
    }

    private static async Task RegisterSupportedIdentifierTypes(ContainerBuilder containerBuilder, Type[] allAssemblyTypes, IEnumerable<Type> registrationProviderAnchorTypes,
        CancellationToken cancellationToken)
    {
        await RegisterCallbackProviders(containerBuilder, registrationProviderAnchorTypes, allAssemblyTypes, cancellationToken);

        RegisterISailfishDependencies(containerBuilder, registrationProviderAnchorTypes);
        RegisterISailfishFixtureGenericTypes(containerBuilder, registrationProviderAnchorTypes);
        RegisterBasicISailfishDependencyTypes(containerBuilder, registrationProviderAnchorTypes);
    }

    private static void RegisterBasicISailfishDependencyTypes(ContainerBuilder containerBuilder, IEnumerable<Type> allAssemblyTypes)
    {
        var basicTypes = allAssemblyTypes.Where(t => t.GetInterfaces().Contains(typeof(ISailfishDependency)));
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
        IEnumerable<Type> registrationProviderAnchorTypes,
        Type[] allAssemblyTypes,
        CancellationToken cancellationToken)
    {
        if (allAssemblyTypes == null) throw new ArgumentNullException(nameof(allAssemblyTypes));
        var registrationProviderAssemblyTypes = registrationProviderAnchorTypes.SelectMany(t => t.Assembly.GetTypes()).Distinct();
        var asyncProviders = GetRegistrationCallbackProviders<IProvideARegistrationCallback>(registrationProviderAssemblyTypes).ToList();
        if (!asyncProviders.Any())
        {
            asyncProviders = GetRegistrationCallbackProviders<IProvideARegistrationCallback>(allAssemblyTypes).ToList();
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

    private static void RegisterISailfishDependencies(ContainerBuilder containerBuilder, IEnumerable<Type> allAssemblyTypes)
    {
        var individualDependencies = allAssemblyTypes.Where(type => type.IsAssignableTo(typeof(ISailfishDependency)));
        foreach (var dependency in individualDependencies)
        {
            if (dependency.GetConstructors().Length > 1 || dependency.GetConstructors().Length > 0 && dependency.GetCtorParamTypes().Length > 0)
            {
                const string errorMessage = $"Implementations of {nameof(ISailfishDependency)} can only have 1 parameterless ctor";
                throw new SailfishException(errorMessage);
            }

            containerBuilder.RegisterType(dependency).AsSelf();
        }
    }
}