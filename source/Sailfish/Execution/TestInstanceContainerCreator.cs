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

    public TestInstanceContainerCreator(
        ITypeResolutionUtility typeResolutionUtility,
        IPropertySetGenerator propertySetGenerator)
    {
        this.typeResolutionUtility = typeResolutionUtility;
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
                typeResolutionUtility,
                testType,
                propertyTensor,
                instanceContainer))
            .ToList();
    }
}