using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.ExtensionMethods;

namespace Sailfish.Execution;

internal class TestInstanceContainerCreator : ITestInstanceContainerCreator
{
    private readonly IParameterGridCreator parameterGridCreator;
    private readonly ITypeResolver? typeResolver;

    public TestInstanceContainerCreator(
        ITypeResolver? typeResolver,
        IParameterGridCreator parameterGridCreator)
    {
        this.typeResolver = typeResolver;
        this.parameterGridCreator = parameterGridCreator;
    }

    public List<TestInstanceContainerProvider> CreateTestContainerInstanceProviders(Type test)
    {
        var (propNames, variableSets) = parameterGridCreator.GenerateParameterGrid(test);
        var methods = test
            .GetMethodsWithAttribute<SailfishMethodAttribute>()
            .OrderBy(x => x.Name);

        return methods.Select(method => new TestInstanceContainerProvider(typeResolver, test, variableSets, propNames, method)).ToList();
    }
}