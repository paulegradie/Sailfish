using System;
using System.Collections.Generic;

namespace Sailfish.Execution;

internal class TestClassResultGroup(Type testClass, List<TestCaseExecutionResult> executionResults)
{
    public Type TestClass { get; } = testClass;
    public List<TestCaseExecutionResult> ExecutionResults { get; } = executionResults;
}