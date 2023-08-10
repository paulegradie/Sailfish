using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

internal class TestInstanceContainerCreator : ITestInstanceContainerCreator
{
    private readonly ITypeActivator typeActivator;
    private readonly IPropertySetGenerator propertySetGenerator;

    public TestInstanceContainerCreator(
        ITypeActivator typeActivator,
        IPropertySetGenerator propertySetGenerator)
    {
        this.typeActivator = typeActivator;
        this.propertySetGenerator = propertySetGenerator;
    }

    public List<TestInstanceContainerProvider> CreateTestContainerInstanceProviders(
        Type testType,
        Func<PropertySet, bool>? propertyTensorFilter = null,
        Func<MethodInfo, bool>? instanceContainerFilter = null)
    {
        var sailfishVariableSets = propertySetGenerator.GenerateSailfishVariableSets(testType, out var variableProperties);
        
        if (propertyTensorFilter is not null)
        {
            sailfishVariableSets = sailfishVariableSets.Where(propertyTensorFilter);
        }

        var sailfishMethods = testType.GetMethodsWithAttribute<SailfishMethodAttribute>();
        if (instanceContainerFilter is not null)
        {
            sailfishMethods = sailfishMethods.Where(instanceContainerFilter);
        }

        return sailfishMethods
            .Select(instanceContainer => new TestInstanceContainerProvider(
                typeActivator,
                testType,
                sailfishVariableSets,
                instanceContainer))
            .ToList();
    }
}