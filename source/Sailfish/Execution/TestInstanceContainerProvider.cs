using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Exceptions;

namespace Sailfish.Execution;

public class TestInstanceContainerProvider
{
    public readonly MethodInfo method;

    private readonly ITypeResolver typeResolver;
    private readonly Type test;
    private readonly Queue<int[]> combos;
    private readonly List<string> propertyNames;
    private readonly Type[] ctorArgTypes;

    public TestInstanceContainerProvider(
        ITypeResolver typeResolver,
        Type test,
        int[][] combos,
        List<string> propertyNames,
        MethodInfo method)
    {
        this.typeResolver = typeResolver;
        this.test = test;
        this.combos = new Queue<int[]>(combos);
        this.propertyNames = propertyNames;
        this.method = method;
        this.ctorArgTypes = test.GetCtorParamTypes();
    }

    private object? instance;

    public int GetNumberCombosInQueue()
    {
        return combos.Count();
    }

    public IEnumerable<TestInstanceContainer> ProvideNextTestInstanceContainer()
    {
        foreach (var nextVariableSet in combos)
        {
            if (instance is null)
            {
                instance = CreateTestInstance();
            }

            SetProperties(instance, nextVariableSet);
            yield return TestInstanceContainer.CreateTestInstance(instance, method, propertyNames.ToArray(), nextVariableSet);
        }
    }

    private void SetProperties(object instance, int[] nextVariableSet)
    {
        foreach (var (propertyName, variableValue) in propertyNames.Zip(nextVariableSet))
        {
            var prop = instance.GetType().GetProperties().Single(x => x.Name == propertyName);
            prop.SetValue(instance, variableValue);
        }
    }

    private object CreateTestInstance()
    {
        var ctorArgs = ctorArgTypes.Select(x => typeResolver.ResolveType(x)).ToArray();
        var instance = Activator.CreateInstance(test, ctorArgs);
        if (instance is null) throw new SailfishException($"Couldn't create instance of {test.Name}");
        return instance;
    }
}