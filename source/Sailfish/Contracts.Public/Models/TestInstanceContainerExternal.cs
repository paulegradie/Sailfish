using System;
using System.Reflection;
using Sailfish.Execution;

namespace Sailfish.Contracts.Public.Models;

public class TestInstanceContainerExternal
{
    public TestInstanceContainerExternal(
        Type type,
        object instance,
        MethodInfo method,
        TestCaseId testCaseId,
        IExecutionSettings executionSettings,
        PerformanceTimer performanceTimer,
        bool disabled)
    {
        Type = type;
        Instance = instance;
        Method = method;
        TestCaseId = testCaseId;
        ExecutionSettings = executionSettings;
        PerformanceTimer = performanceTimer;
        Disabled = disabled;
    }

    public Type Type { get; }

    public object Instance { get; }

    public MethodInfo Method { get; }

    public TestCaseId TestCaseId { get; }

    public IExecutionSettings ExecutionSettings { get; }

    public PerformanceTimer PerformanceTimer { get; }

    public bool Disabled { get; }
}