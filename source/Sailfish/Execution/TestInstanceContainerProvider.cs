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
    private readonly Queue<int[]> variableSets;
    private readonly List<string> propertyNames;

    public TestInstanceContainerProvider(
        ITypeResolver? typeResolver,
        Type test,
        IEnumerable<int[]> variableSets,
        List<string> propertyNames,
        MethodInfo method)
    {
        this.typeResolver = typeResolver;
        this.test = test;
        this.variableSets = new Queue<int[]>(variableSets.OrderBy(x => Guid.NewGuid()));
        this.propertyNames = propertyNames;
        Method = method;
    }

    public int GetNumberOfVariableSetsInTheQueue()
    {
        return variableSets.Count;
    }

    public IEnumerable<TestInstanceContainer> ProvideNextTestInstanceContainer()
    {
        foreach (var nextVariableSet in variableSets)
        {
            var instance = CreateTestInstance();

            SetProperties(instance, nextVariableSet);
            yield return TestInstanceContainer.CreateTestInstance(instance, Method, propertyNames.ToArray(), nextVariableSet);
        }
    }

    private void SetProperties(object obj, IEnumerable<int> nextVariableSet)
    {
        foreach (var (propertyName, variableValue) in propertyNames.Zip(nextVariableSet))
        {
            var prop = obj.GetType().GetProperties().Single(x => x.Name == propertyName);
            prop.SetValue(obj, variableValue);
        }
    }

    private object CreateTestInstance()
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