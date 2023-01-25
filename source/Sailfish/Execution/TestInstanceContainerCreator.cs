using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.ExtensionMethods;

namespace Sailfish.Execution;

internal class TestInstanceContainerCreator : ITestInstanceContainerCreator
{
    private readonly ITypeResolutionUtility typeResolutionUtility;
    private readonly IPropertySetGenerator propertySetGenerator;
    private readonly IEnumerable<Type> additionalAnchorTypes;

    public TestInstanceContainerCreator(
        ITypeResolutionUtility typeResolutionUtility,
        IPropertySetGenerator propertySetGenerator,
        IEnumerable<Type> additionalAnchorTypes)
    {
        this.typeResolutionUtility = typeResolutionUtility;
        this.propertySetGenerator = propertySetGenerator;
        this.additionalAnchorTypes = additionalAnchorTypes;
    }

    public List<TestInstanceContainerProvider> CreateTestContainerInstanceProviders(
        Type test,
        Func<PropertySet, bool>? propertySetFilter = null,
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

        return methods
            .OrderBy(x => x.Name)
            .Select(method => new TestInstanceContainerProvider(typeResolutionUtility, test, propertySets, method, additionalAnchorTypes))
            .ToList();
    }
}