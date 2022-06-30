using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Attributes;
using Sailfish.Utils;

namespace Sailfish.Execution;

public class TestInstanceContainerCreator : ITestInstanceContainerCreator
{
    private readonly IParameterGridCreator parameterGridCreator;
    private readonly ITypeResolver typeResolver;

    public TestInstanceContainerCreator(
        ITypeResolver typeResolver,
        IParameterGridCreator parameterGridCreator)
    {
        this.typeResolver = typeResolver;
        this.parameterGridCreator = parameterGridCreator;
    }

    public List<TestInstanceContainerProvider> CreateTestContainerInstanceProvider(Type test)
    {
        var (propNames, combos) = parameterGridCreator.GenerateParameterGrid(test);
        var methods = test
            .GetMethodsWithAttribute<ExecutePerformanceCheckAttribute>()
            .OrderBy(x => x.Name);

        var instanceContainers = new List<TestInstanceContainerProvider>();
        foreach (var method in methods)
        {
            foreach (var combo in combos)
            {
                var provider = new TestInstanceContainerProvider(
                    typeResolver,
                    test,
                    combo,
                    propNames,
                    method);
                instanceContainers.Add(provider);
            }
        }

        return instanceContainers;
    }
}