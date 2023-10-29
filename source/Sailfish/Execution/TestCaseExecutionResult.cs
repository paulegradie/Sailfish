using System;

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

    public TestCaseExecutionResult(IExecutionSettings executionSettings, Exception exception)
    {
        Exception = exception;
        IsSuccess = false;
        StatusCode = StatusCode.Failure;
        ExecutionSettings = executionSettings;

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