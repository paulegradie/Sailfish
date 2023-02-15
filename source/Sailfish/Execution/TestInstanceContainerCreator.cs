using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.ExtensionMethods;

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
        var propertyTensor = propertySetGenerator.GeneratePropertySets(testType);
        if (propertyTensorFilter is not null)
        {
            propertyTensor = propertyTensor.Where(propertyTensorFilter);
        }

        var instanceContainers = testType.GetMethodsWithAttribute<SailfishMethodAttribute>();
        if (instanceContainerFilter is not null)
        {
            instanceContainers = instanceContainers.Where(instanceContainerFilter);
        }

        return instanceContainers
            .OrderBy(x => x.Name)
            .Select(instanceContainer => new TestInstanceContainerProvider(
                typeActivator,
                testType,
                propertyTensor,
                instanceContainer))
            .ToList();
    }
}