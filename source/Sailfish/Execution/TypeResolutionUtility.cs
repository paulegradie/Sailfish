using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Autofac;
using Sailfish.AdapterUtils;
using Sailfish.Exceptions;
using Sailfish.ExtensionMethods;
using Sailfish.Registration;

namespace Sailfish.Execution;

public class TypeResolutionUtility : ITypeResolutionUtility
{
    private readonly List<ITypeResolver> typeResolvers = new();

    public async Task<object> CreateDehydratedTestInstance(Type test, IEnumerable<Type> testDiscoveryAnchorTypes, IEnumerable<Type> registrationProviderAnchorTypes)
    {
        var allAssemblies = new List<Assembly> { test.Assembly };
        allAssemblies.AddRange(testDiscoveryAnchorTypes.Select(t => t.Assembly));
        var allAssemblyTypes = AssemblyScannerCache.GetTypesInAssemblies(allAssemblies.Distinct()).ToArray();

        // 1. Search for ISailfishFixtureDependency as generic args
        typeResolvers.AddRange(test.GetSailfishFixtureGenericArguments());

        // // 2. Search for IProvideTypesToSailfish
        typeResolvers.AddRange(allAssemblyTypes.GetIProvideTypesToSailfishInstances());

        // 3. Search for registration callbacks
        var containerBuilder = new ContainerBuilder();
        var registrationProviderAssemblyTypes = registrationProviderAnchorTypes.SelectMany(t => t.Assembly.GetTypes()).Distinct();
        var asyncProviders = registrationProviderAssemblyTypes.GetRegistrationCallbackProviders<IProvideARegistrationCallback>().ToList();
        if (!asyncProviders.Any())
        {
            asyncProviders = allAssemblyTypes.GetRegistrationCallbackProviders<IProvideARegistrationCallback>().ToList();
        }

        foreach (var asyncCallback in asyncProviders)
        {
            var methodInfo = asyncCallback.GetType().GetMethod(nameof(IProvideARegistrationCallback.RegisterAsync));
            if (methodInfo is not null && methodInfo.IsAsyncMethod())
            {
                await asyncCallback.RegisterAsync(containerBuilder).ConfigureAwait(false);
            }
            else
            {
                var task = asyncCallback.RegisterAsync(containerBuilder);
                if (!task.IsCompletedSuccessfully)
                {
                    throw new Exception("Task error in your registration callback");
                }
            }
        }

        // 4. Look for all types that implement the ISailfishDependency interface - should have no ctor args or throw
        var individualDependencies = allAssemblyTypes.Where(type => type.IsAssignableTo(typeof(ISailfishDependency)) || type.IsAssignableTo(typeof(ISailfishFixtureDependency)));
        foreach (var dependency in individualDependencies)
        {
            const string errorMessage = $"Implementations of {nameof(ISailfishDependency)} can only have 1 parameterless ctor";
            if (dependency.GetConstructors().Length > 1 || dependency.GetConstructors().Length > 0 && dependency.GetCtorParamTypes().Length > 0)
            {
                throw new SailfishException(errorMessage);
            }

            containerBuilder.RegisterType(dependency).AsSelf();
        }

        var container = containerBuilder.Build();

        typeResolvers.Add(new LifetimeScopeTypeResolver(container.BeginLifetimeScope()));

        var ctorArgTypes = test.GetCtorParamTypes();

        var ctorArgs = ctorArgTypes.Select(ResolveObjectWrapper).ToArray();
        var obj = Activator.CreateInstance(test, ctorArgs);
        if (obj is null) throw new SailfishException($"Couldn't create instance of {test.Name}");
        return obj;
    }

    private object ResolveObjectWrapper(Type type)
    {
        object? instance = null;

        foreach (var resolver in typeResolvers)
        {
            try
            {
                instance = resolver.ResolveType(type);
                break;
            }
            catch
            {
                // ignored
            }
        }

        if (instance is null)
        {
            throw new SailfishException($"No way found to resolve type: {type.Name}");
        }

        return instance;
    }
}