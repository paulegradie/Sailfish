using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Utils;

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

    private static bool TestIsDisabled(MemberInfo test, MemberInfo method)
    {
        var typeIsDisabled = test.GetCustomAttributes<SailfishAttribute>().Single().Disabled;
        var methodIsDisabled = method.GetCustomAttributes<SailfishMethodAttribute>().Single().Disabled;
        return (methodIsDisabled || typeIsDisabled);
    }

    public IEnumerable<TestInstanceContainer> ProvideNextTestInstanceContainer()
    {
        var disabled = TestIsDisabled(Test, Method);
        if (GetNumberOfPropertySetsInTheQueue() is 0)
        {
            var testCaseId = DisplayNameHelper.CreateTestCaseId(Test, Method.Name, Array.Empty<string>(), Array.Empty<object>()); // a uniq id
            var instance = typeActivator.CreateDehydratedTestInstance(Test, testCaseId, disabled);
            yield return TestInstanceContainer.CreateTestInstance(instance, Method, Array.Empty<string>(), Array.Empty<object>(), disabled);
        }
        else
        {
            foreach (var nextPropertySet in propertySets)
            {
                var propertyNames = nextPropertySet.GetPropertyNames().ToArray();
                var variableValues = nextPropertySet.GetPropertyValues().ToArray();
                var testCaseId = DisplayNameHelper.CreateTestCaseId(Test, Method.Name, propertyNames, variableValues); // a uniq id

                var instance = typeActivator.CreateDehydratedTestInstance(Test, testCaseId, disabled);
                HydrateInstanceTestProperties(instance, nextPropertySet);

                yield return TestInstanceContainer.CreateTestInstance(instance, Method, propertyNames, variableValues, disabled);
            }
        }
    }

    private static void HydrateInstanceTestProperties(object obj, PropertySet propertySet)
    {
        foreach (var variable in propertySet.VariableSet)
        {
            var prop = obj.GetType().GetProperties().Single(x => x.Name == variable.Name);
            prop.SetValue(obj, variable.Value);
        }
    }
}