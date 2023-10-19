using System;
using System.Collections.Generic;

namespace Sailfish.Execution;

internal class TestClassResultGroup
{
    public TestClassResultGroup(Type testClass, List<TestCaseExecutionResult> executionResults)
    {
        TestClass = testClass;
        ExecutionResults = executionResults;
    }

    public Type TestClass { get; }
    public List<TestCaseExecutionResult> ExecutionResults { get; }
}