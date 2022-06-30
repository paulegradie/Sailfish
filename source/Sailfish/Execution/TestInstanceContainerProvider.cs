using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Exceptions;

namespace Sailfish.Execution;

// TODO: register this 
public class TestInstanceContainerProvider
{
    public readonly MethodInfo method;

    private readonly ITypeResolver typeResolver;
    private readonly Type test;
    private readonly int[] combo;
    private readonly List<string> propertyNames;
    private readonly Type[] ctorArgTypes;

    public TestInstanceContainerProvider(
        ITypeResolver typeResolver,
        Type test,
        int[] combo,
        List<string> propertyNames,
        MethodInfo method)
    {
        this.typeResolver = typeResolver;
        this.test = test;
        this.combo = combo;
        this.propertyNames = propertyNames;
        this.method = method;
        this.ctorArgTypes = test.GetCtorParamTypes();
    }

    public TestInstanceContainer ProvideTestInstanceContainer()
    {
        var instance = CreateTestInstance();
        SetProperties(instance);

        var container = TestInstanceContainer.CreateTestInstance(instance, method, propertyNames.ToArray(), combo);
        return container;
    }

    private void SetProperties(object instance)
    {
        foreach (var (propertyName, variableValue) in propertyNames.Zip(combo))
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