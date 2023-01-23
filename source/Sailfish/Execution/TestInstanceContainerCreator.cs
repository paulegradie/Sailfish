using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.ExtensionMethods;

namespace Sailfish.Execution;

internal class TestInstanceContainerCreator : ITestInstanceContainerCreator
{
    private readonly IPropertySetGenerator propertySetGenerator;
    private readonly ITypeResolver? typeResolver;

    public TestInstanceContainerCreator(
        ITypeResolver? typeResolver,
        IPropertySetGenerator propertySetGenerator)
    {
        this.typeResolver = typeResolver;
        this.propertySetGenerator = propertySetGenerator;
    }

    public List<TestInstanceContainerProvider> CreateTestContainerInstanceProviders(Type test, Func<PropertySet, bool>? propertySetFilter = null,
        Func<MethodInfo, bool>? methodFilter = null)
    {
        var propertySets = propertySetGenerator.GeneratePropertySets(test);
        if (propertySetFilter is not null)
        {
            propertySets = propertySets.Where(propertySetFilter);
        }

        var methods = test.GetMethodsWithAttribute<SailfishMethodAttribute>();

        if (methodFilter is not null)
        {
            methods = methods.Where(methodFilter);
        }


        return methods.OrderBy(x => x.Name).Select(method => new TestInstanceContainerProvider(typeResolver, test, propertySets, method)).ToList();
    }
}