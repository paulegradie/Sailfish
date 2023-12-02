using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sailfish.Attributes;
using Sailfish.Contracts.Public;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

internal interface ITestInstanceContainerCreator
{
    List<TestInstanceContainerProvider> CreateTestContainerInstanceProviders(
        Type test,
        Func<PropertySet, bool>? propertyTensorFilter = null,
        Func<MethodInfo, bool>? instanceContainerFilter = null);
}

internal class TestInstanceContainerCreator : ITestInstanceContainerCreator
{
    private readonly IRunSettings runSettings;
    private readonly ITypeActivator typeActivator;
    private readonly IPropertySetGenerator propertySetGenerator;

    public TestInstanceContainerCreator(
        IRunSettings runSettings,
        ITypeActivator typeActivator,
        IPropertySetGenerator propertySetGenerator)
    {
        this.runSettings = runSettings;
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

        var sailfishMethods = testType
            .GetMethodsWithAttribute<SailfishMethodAttribute>()
            .OrderBy(x => x.GetCustomAttribute<SailfishMethodAttribute>()?.Order).ToList();

        if (instanceContainerFilter is not null)
        {
            sailfishMethods = sailfishMethods.Where(instanceContainerFilter).ToList();
        }

        return sailfishMethods
            .Select(instanceContainer => new TestInstanceContainerProvider(
                runSettings,
                typeActivator,
                testType,
                sailfishVariableSets,
                instanceContainer))
            .ToList();
    }
}