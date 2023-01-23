using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.AdapterUtils;
using Sailfish.Exceptions;
using Sailfish.ExtensionMethods;

namespace Sailfish.Execution;

internal class TestInstanceContainerProvider
{
    public readonly MethodInfo Method;

    private readonly ITypeResolver? typeResolver;
    private readonly Type test;
    private readonly IEnumerable<PropertySet> propertySets;

    public TestInstanceContainerProvider(
        ITypeResolver? typeResolver,
        Type test,
        IEnumerable<PropertySet> propertySets,
        MethodInfo method)
    {
        this.typeResolver = typeResolver;
        this.test = test;
        this.propertySets = propertySets;
        Method = method;
    }

    public int GetNumberOfPropertySetsInTheQueue()
    {
        return propertySets.Count();
    }

    public IEnumerable<TestInstanceContainer> ProvideNextTestInstanceContainer()
    {
        if (GetNumberOfPropertySetsInTheQueue() is 0)
        {
            var instance = CreateDehydratedTestInstance();
            yield return TestInstanceContainer.CreateTestInstance(instance, Method, Array.Empty<string>(), Array.Empty<int>());
        }

        foreach (var nextPropertySet in propertySets)
        {
            var instance = CreateDehydratedTestInstance();

            HydrateInstance(instance, nextPropertySet);

            var propertyNames = nextPropertySet.GetPropertyNames().ToArray();
            var variableValues = nextPropertySet.GetPropertyValues().ToArray();
            yield return TestInstanceContainer.CreateTestInstance(instance, Method, propertyNames, variableValues);
        }
    }

    private static void HydrateInstance(object obj, PropertySet propertySet)
    {
        foreach (var variable in propertySet.VariableSet)
        {
            var prop = obj.GetType().GetProperties().Single(x => x.Name == variable.Name);
            prop.SetValue(obj, variable.Value);
        }
    }

    private object CreateDehydratedTestInstance()
    {
        var sailfishFixtureDependency = test.GetSailfishFixtureGenericArgument();

        var ctorArgTypes = test.GetCtorParamTypes();
        var ctorArgs = ctorArgTypes.Select(x => ResolveObjectWrapper(x, sailfishFixtureDependency)).ToArray();
        var obj = Activator.CreateInstance(test, ctorArgs);
        if (obj is null) throw new SailfishException($"Couldn't create instance of {test.Name}");
        return obj;
    }

    private object ResolveObjectWrapper(Type type, ISailfishFixtureDependency? sailfishFixtureDependency)
    {
        var fixtureDependencyWasNull = sailfishFixtureDependency is null;
        var typeResolverWasNull = typeResolver is null;

        if (sailfishFixtureDependency is not null)
        {
            try
            {
                var typeInstance = sailfishFixtureDependency.ResolveType(type);
                return typeInstance;
            }
            catch
            {
                try
                {
                    if (typeResolver is null) throw;
                    var typeInstance = typeResolver.ResolveType(type);
                    return typeInstance;
                }
                catch (Exception ex)
                {
                    throw new SailfishException(ex.Message);
                }
            }
        }

        try
        {
            var typeInstance = typeResolver!.ResolveType(type);
            return typeInstance;
        }
        catch (Exception ex)
        {
            throw new SailfishException(
                $"No way found to resolve type: {type.Name} - {ex.Message}... fixtureDependencyWasNull was {fixtureDependencyWasNull}, and typeResolverWasNull was {typeResolverWasNull}");
        }
    }
}

public static class FixtureGenericArgumentExtensionMethods
{
    public static ISailfishFixtureDependency? GetSailfishFixtureGenericArgument(this Type test)
    {
        var sailfishFixtureType = test
            .GetInterfaces()
            .SingleOrDefault(x => x.GenericTypeArguments.Length > 0);

        if (sailfishFixtureType is null) return null;
        var fixtureType = sailfishFixtureType.GetGenericArguments()?.Single()!;
        var fixtureDependencyInstance = Activator.CreateInstance(fixtureType);
        return fixtureDependencyInstance as ISailfishFixtureDependency;
    }
}