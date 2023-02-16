using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sailfish.Execution;

internal class TestInstanceContainerProvider
{
    public readonly MethodInfo Method;
    public readonly Type Test;
    private readonly ITypeActivator typeActivator;
    private readonly IEnumerable<PropertySet> propertySets;

    public TestInstanceContainerProvider(
        ITypeActivator typeActivator,
        Type test,
        IEnumerable<PropertySet> propertySets,
        MethodInfo method)
    {
        Method = method;
        Test = test;

        this.typeActivator = typeActivator;
        this.propertySets = propertySets;
    }

    public int GetNumberOfPropertySetsInTheQueue()
    {
        return propertySets.Count();
    }

    public IEnumerable<TestInstanceContainer> ProvideNextTestInstanceContainer()
    {
        if (GetNumberOfPropertySetsInTheQueue() is 0)
        {
            var instance = typeActivator.CreateDehydratedTestInstance(Test);
            yield return TestInstanceContainer.CreateTestInstance(instance, Method, Array.Empty<string>(), Array.Empty<int>());
        }
        else
        {
            foreach (var nextPropertySet in propertySets)
            {
                var instance = typeActivator.CreateDehydratedTestInstance(Test);

                HydrateInstance(instance, nextPropertySet);

                var propertyNames = nextPropertySet.GetPropertyNames().ToArray();
                var variableValues = nextPropertySet.GetPropertyValues().ToArray();
                yield return TestInstanceContainer.CreateTestInstance(instance, Method, propertyNames, variableValues);
            }
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
}