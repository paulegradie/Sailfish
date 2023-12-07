using Sailfish.Contracts.Public.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Execution;

public interface IClassExecutionSummary
{
    Type TestClass { get; }
    IExecutionSettings ExecutionSettings { get; }
    IEnumerable<ICompiledTestCaseResult> CompiledTestCaseResults { get; }

    IEnumerable<ICompiledTestCaseResult> GetSuccessfulTestCases();

    IEnumerable<ICompiledTestCaseResult> GetFailedTestCases();

    IClassExecutionSummary FilterForSuccessfulTestCases();

    IClassExecutionSummary FilterForFailureTestCases();
}

internal class ClassExecutionSummary(
    Type type,
    IExecutionSettings executionSettings,
    IEnumerable<ICompiledTestCaseResult> compiledResults) : IClassExecutionSummary
{
    public Type TestClass { get; } = type;
    public IExecutionSettings ExecutionSettings { get; } = executionSettings;
    public IEnumerable<ICompiledTestCaseResult> CompiledTestCaseResults { get; } = compiledResults;

    public IEnumerable<ICompiledTestCaseResult> GetSuccessfulTestCases()
    {
        return CompiledTestCaseResults.Where(x => x.PerformanceRunResult is not null);
    }

    public IEnumerable<ICompiledTestCaseResult> GetFailedTestCases()
    {
        return CompiledTestCaseResults.Where(x => x.PerformanceRunResult is null);
    }

    public IClassExecutionSummary FilterForSuccessfulTestCases()
    {
        return new ClassExecutionSummary(TestClass, ExecutionSettings, GetSuccessfulTestCases());
    }

    public IClassExecutionSummary FilterForFailureTestCases()
    {
        return new ClassExecutionSummary(TestClass, ExecutionSettings, GetFailedTestCases());
    }
}