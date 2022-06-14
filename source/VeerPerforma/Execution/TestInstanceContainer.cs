using System.Reflection;
using VeerPerforma.Utils;

namespace VeerPerforma.Execution;

public class TestInstanceContainer
{
    private TestInstanceContainer(
        Type type,
        object instance,
        MethodInfo method,
        string displayName,
        int numWarmupIterations,
        int numIterations)
    {
        Type = type;
        Instance = instance;
        ExecutionMethod = method;
        DisplayName = displayName;
        NumIterations = numIterations;
        NumWarmupIterations = numWarmupIterations;
    }

    public Type Type { get; private set; }
    public object Instance { get; private set; }
    public MethodInfo ExecutionMethod { get; private set; }
    public string DisplayName { get; private set; } // This is a uniq id since we take a Distinct on all Iteration Variable attribute param -- class.method(varA: 1, varB: 3) is the form
    public int NumWarmupIterations { get; private set; }
    public int NumIterations { get; private set; }

    public AncillaryInvocation Invocation => new(Instance, ExecutionMethod, new PerformanceTimer());

    public static TestInstanceContainer CreateTestInstance(object instance, MethodInfo method, string[] propertyNames, int[] variables)
    {
        if (propertyNames.Length != variables.Length) throw new Exception("Property names and variables do not match");

        var paramsDisplay = DisplayNameHelper.CreateParamsDisplay(propertyNames, variables);
        var fullDisplayName = DisplayNameHelper.CreateDisplayName(instance.GetType(), method.Name, paramsDisplay); // a uniq id
        var numWarmupIterations = instance.GetType().GetWarmupIterations();
        var numIterations = instance.GetType().GetNumIterations();

        return new TestInstanceContainer(instance.GetType(), instance, method, fullDisplayName, numWarmupIterations, numIterations);
    }
}