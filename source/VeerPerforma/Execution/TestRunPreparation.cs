using System.Reflection;

namespace VeerPerforma.Execution;

public class TestRunPreparation : ITestRunPreparation
{
    private readonly IMethodOrganizer methodOrganizer;
    private readonly IInstanceCreator instanceCreator;
    private readonly IParameterGridCreator parameterGridCreator;

    public TestRunPreparation(
        IMethodOrganizer methodOrganizer,
        IInstanceCreator instanceCreator,
        IParameterGridCreator parameterGridCreator
    )
    {
        this.methodOrganizer = methodOrganizer;
        this.instanceCreator = instanceCreator;
        this.parameterGridCreator = parameterGridCreator;
    }

    public Dictionary<string, List<(MethodInfo, object)>> GenerateTestInstances(Type test)
    {
        var (propNames, combos) = parameterGridCreator.GenerateParameterGrid(test);
        var instances = instanceCreator.CreateInstances(test, combos, propNames);
        var methodMap = methodOrganizer.FormMethodGroups(instances);
        return methodMap;
    }
}