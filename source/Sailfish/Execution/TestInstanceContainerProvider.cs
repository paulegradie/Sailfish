using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Exceptions;
using Sailfish.ExtensionMethods;

namespace Sailfish.Execution;

internal class TestInstanceContainerProvider
{
    public readonly MethodInfo Method;

    private readonly ITypeResolver typeResolver;
    private readonly Type test;
    private readonly Queue<int[]> variableSets;
    private readonly List<string> propertyNames;
    private readonly Type[] ctorArgTypes;

    public TestInstanceContainerProvider(
        ITypeResolver typeResolver,
        Type test,
        IEnumerable<int[]> variableSets,
        List<string> propertyNames,
        MethodInfo method)
    {
        this.typeResolver = typeResolver;
        this.test = test;
        this.variableSets = new Queue<int[]>(variableSets);
        this.propertyNames = propertyNames;
        Method = method;
        ctorArgTypes = test.GetCtorParamTypes();
    }

    private object? instance;

    public int GetNumberOfVariableSetsInTheQueue()
    {
        return variableSets.Count;
    }

    public IEnumerable<TestInstanceContainer> ProvideNextTestInstanceContainer()
    {
        foreach (var nextVariableSet in variableSets)
        {
            instance ??= CreateTestInstance();

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
        var ctorArgs = ctorArgTypes.Select(x => typeResolver.ResolveType(x)).ToArray();
        var obj = Activator.CreateInstance(test, ctorArgs);
        if (obj is null) throw new SailfishException($"Couldn't create instance of {test.Name}");
        return obj;
    }
}