using System;
using System.Collections.Generic;

namespace Sailfish.Execution;

internal class RawExecutionResult
{
    public RawExecutionResult(Type testType, List<TestExecutionResult> executionResults)
    {
        IsSuccess = true;
        TestType = testType;
        ExecutionResults = executionResults;
    }


    public RawExecutionResult(Type testType, Exception exception)
    {
        IsSuccess = false;
        Exception = exception;
        TestType = testType;
    }

    public Exception? Exception { get; }
    public bool IsSuccess { get; }
    public Type TestType { get; }
    public List<TestExecutionResult>? ExecutionResults { get; }
}