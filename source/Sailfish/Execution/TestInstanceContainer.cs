using Sailfish.Contracts.Public.Models;
using Sailfish.Utils;
using System;
using System.Reflection;

namespace Sailfish.Execution;

/// <summary>
///     A test instance container is a single case for a single method for a single type
///     It will contain all of the necessary items to execute a test case
/// </summary>
internal class TestInstanceContainer
{
    private TestInstanceContainer(
        Type type,
        object instance,
        MethodInfo method,
        TestCaseId testCaseId,
        IExecutionSettings executionSettings,
        CoreInvoker coreInvoker,
        bool disabled)
    {
        Type = type;
        Instance = instance;
        ExecutionMethod = method;
        TestCaseId = testCaseId;
        ExecutionSettings = executionSettings;
        CoreInvoker = coreInvoker;
        Disabled = disabled;
        GroupingId = $"{instance.GetType().Name}.{method.Name}";
    }

    public Type Type { get; }
    public object Instance { get; set; }
    public MethodInfo ExecutionMethod { get; }
    public string GroupingId { get; set; }
    public TestCaseId TestCaseId { get; } // This is a uniq id since we take a Distinct on all Iteration Variable attribute param -- class.method(varA: 1, varB: 3) is the form
    public int NumWarmupIterations => ExecutionSettings.NumWarmupIterations;
    public int SampleSize => ExecutionSettings.SampleSize;

    public IExecutionSettings ExecutionSettings { get; }

    public CoreInvoker CoreInvoker { get; }
    public bool Disabled { get; }

    public static TestInstanceContainer CreateTestInstance(
        object instance,
        MethodInfo method,
        string[] propertyNames,
        object[] variables,
        bool disabled,
        IExecutionSettings executionSettings
    )
    {
        if (propertyNames.Length != variables.Length) throw new Exception("Property names and variables do not match");

        var testCaseId = DisplayNameHelper.CreateTestCaseId(instance.GetType(), method.Name, propertyNames, variables); // a uniq id

        return new TestInstanceContainer(
            instance.GetType(),
            instance,
            method,
            testCaseId,
            executionSettings,
            new CoreInvoker(instance, method, new PerformanceTimer()),
            disabled);
    }

    public void ApplyOverheadEstimates(int overheadEstimate)
    {
        CoreInvoker.AssignOverheadEstimate(overheadEstimate);
    }

    public TestInstanceContainerExternal ToExternal()
    {
        return new TestInstanceContainerExternal(Type, Instance, ExecutionMethod, TestCaseId, ExecutionSettings, CoreInvoker.GetPerformanceResults(), Disabled);
    }
}