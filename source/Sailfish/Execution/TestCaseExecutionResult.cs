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

        // TestInstanceContainerProvider = null;
        TestInstanceContainer = container;
        ExecutionSettings = container.ExecutionSettings;
        PerformanceTimerResults = container.CoreInvoker.GetPerformanceResults(false);
    }

    public TestCaseExecutionResult(Exception exception)
    {
        Exception = exception;
        IsSuccess = false;
        StatusCode = StatusCode.Failure;

        TestInstanceContainer = null;
        ExecutionSettings = null;
        PerformanceTimerResults = null;
    }

    public IExecutionSettings? ExecutionSettings { get; }
    public Exception? Exception { get; }
    public StatusCode StatusCode { get; }
    public bool IsSuccess { get; }
    public TestInstanceContainer? TestInstanceContainer { get; }
    public PerformanceTimer? PerformanceTimerResults { get; }
}