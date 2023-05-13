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
        Exceptions.Add(exception);
        TestType = testType;
    }

    public RawExecutionResult(Type testType, IEnumerable<Exception> exceptions)
    {
        IsSuccess = false;
        Exceptions.AddRange(exceptions);
        TestType = testType;
    }

    public List<Exception> Exceptions { get; } = new();
    public bool IsSuccess { get; }
    public Type TestType { get; }
    public List<TestExecutionResult>? ExecutionResults { get; }
}