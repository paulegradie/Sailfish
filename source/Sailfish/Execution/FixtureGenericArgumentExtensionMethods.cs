using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.AdapterUtils;
using Sailfish.Registration;

namespace Sailfish.Execution;

public static class FixtureGenericArgumentExtensionMethods
{
    public static List<ISailfishFixtureDependency> GetSailfishFixtureGenericArguments(this Type test)
    {
        var sailfishFixtureTypes = test
            .GetInterfaces()
            .Where(x => x.GenericTypeArguments.Length > 0)
            .Where(x => x.GenericTypeArguments.All(arg => arg.IsAssignableFrom(typeof(ISailfishFixtureDependency))));

        var instances = new List<ISailfishFixtureDependency>();
        foreach (var sailfishFixtureType in sailfishFixtureTypes)
        {
            var fixtureType = sailfishFixtureType.GetGenericArguments()?.Single()!;
            if (Activator.CreateInstance(fixtureType) is ISailfishFixtureDependency fixtureDependencyInstance)
            {
                instances.Add(fixtureDependencyInstance);
            }
        }

        return instances;
    }

    public static IEnumerable<ITypeResolver> GetIProvideTypesToSailfishInstances(this IEnumerable<Type> allAssemblyTypes)
    {
        // 2. Search for IProvideTypesToSailfish
        return allAssemblyTypes
            .Where(type => type.GetInterfaces().Contains(typeof(IProvideTypesToSailfish)))
            .Select(Activator.CreateInstance)
            .Cast<ITypeResolver>();
    }

    public static IEnumerable<IProvideARegistrationCallback> GetRegistrationCallbackProviders(this IEnumerable<Type> allAssemblyTypes)
    {
        // 3. Search for Registration Callback types
        return allAssemblyTypes
            .Where(type => type.GetInterfaces().Contains(typeof(IProvideARegistrationCallback)))
            .Select(Activator.CreateInstance)
            .Cast<IProvideARegistrationCallback>();
    }

    public static IEnumerable<Type> RetrieveAdditionalAssemblyTypes(this IEnumerable<Type> additionalAnchorTypes)
    {
        return additionalAnchorTypes.SelectMany(t => t.Assembly.GetTypes());
    }
}