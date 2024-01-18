using Sailfish.Attributes;
using Sailfish.Contracts.Public.Models;
using Sailfish.Extensions.Methods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sailfish.Execution;

internal interface ITestInstanceContainerCreator
{
    List<TestInstanceContainerProvider> CreateTestContainerInstanceProviders(
        Type test,
        Func<PropertySet, bool>? propertyTensorFilter = null,
        Func<MethodInfo, bool>? instanceContainerFilter = null);
}

internal class TestInstanceContainerCreator(
    IRunSettings runSettings,
    ITypeActivator typeActivator,
    IPropertySetGenerator propertySetGenerator) : ITestInstanceContainerCreator
{
    private readonly IPropertySetGenerator propertySetGenerator = propertySetGenerator;
    private readonly IRunSettings runSettings = runSettings;
    private readonly ITypeActivator typeActivator = typeActivator;

    public List<TestInstanceContainerProvider> CreateTestContainerInstanceProviders(
        Type testType,
        Func<PropertySet, bool>? propertyTensorFilter = null,
        Func<MethodInfo, bool>? instanceContainerFilter = null)
    {
        var sailfishVariableSets = propertySetGenerator.GenerateSailfishVariableSets(testType, out var variableProperties);

        if (propertyTensorFilter is not null) sailfishVariableSets = sailfishVariableSets.Where(propertyTensorFilter);

        var sailfishMethods = testType
            .GetMethodsWithAttribute<SailfishMethodAttribute>()
            .OrderBy(x => x.GetCustomAttribute<SailfishMethodAttribute>()?.Order).ToList();

        if (instanceContainerFilter is not null) sailfishMethods = sailfishMethods.Where(instanceContainerFilter).ToList();

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