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
                instanceContainer,
                additionalAnchorTypes))
            .ToList();
    }
}