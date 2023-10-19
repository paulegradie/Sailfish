using System;
using Sailfish.Extensions.Methods;

namespace Sailfish.Execution;

internal class TestCaseExecutionResult
{
    public TestCaseExecutionResult(TestInstanceContainer container)
    {
        Exception = null;
        IsSuccess = true;
        StatusCode = StatusCode.Success;

        TestInstanceContainer = container;
        ExecutionSettings = container.ExecutionSettings;
        PerformanceTimerResults = container.CoreInvoker.GetPerformanceResults();
    }

    public TestCaseExecutionResult(TestInstanceContainer container, Exception exception)
    {
        Exception = exception;
        IsSuccess = false;
        StatusCode = StatusCode.Failure;

        TestInstanceContainer = container;
        ExecutionSettings = container.ExecutionSettings;
        PerformanceTimerResults = container.CoreInvoker.GetPerformanceResults(false);
    }

    public TestCaseExecutionResult(TestInstanceContainerProvider testProvider, Exception exception)
    {
        Exception = exception;
        IsSuccess = false;
        StatusCode = StatusCode.Failure;

        TestInstanceContainer = null;
        TestInstanceContainerProvider = testProvider;
        ExecutionSettings = null;
        PerformanceTimerResults = null;
    }

    public TestCaseExecutionResult(Type testType, Exception exception)
    {
        Exception = exception;
        IsSuccess = false;
        StatusCode = StatusCode.Failure;
        ExecutionSettings = testType.RetrieveExecutionTestSettings();

        TestInstanceContainer = null;
        TestInstanceContainerProvider = null;
        PerformanceTimerResults = null;
    }

    public IExecutionSettings? ExecutionSettings { get; }
    public Exception? Exception { get; }
    public StatusCode StatusCode { get; }
    public bool IsSuccess { get; }
    public TestInstanceContainer? TestInstanceContainer { get; set; }
    public TestInstanceContainerProvider? TestInstanceContainerProvider { get; set; }
    public PerformanceTimer? PerformanceTimerResults { get; }
}