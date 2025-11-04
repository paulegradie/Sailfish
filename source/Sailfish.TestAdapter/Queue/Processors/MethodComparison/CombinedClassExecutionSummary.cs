using Sailfish.Contracts.Public.Models;
using Sailfish.Execution;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.TestAdapter.Queue.Processors.MethodComparison;

/// <summary>
/// A combined class execution summary that merges results from multiple individual test case summaries.
/// This is needed for SailDiff comparisons which require access to both before and after test results.
/// </summary>
internal class CombinedClassExecutionSummary : IClassExecutionSummary
{
    public CombinedClassExecutionSummary(
        Type testClass,
        IExecutionSettings executionSettings,
        IEnumerable<ICompiledTestCaseResult> compiledTestCaseResults)
    {
        TestClass = testClass;
        ExecutionSettings = executionSettings;
        CompiledTestCaseResults = compiledTestCaseResults;
    }

    public Type TestClass { get; }
    public IExecutionSettings ExecutionSettings { get; }
    public IEnumerable<ICompiledTestCaseResult> CompiledTestCaseResults { get; }

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
        return new CombinedClassExecutionSummary(TestClass, ExecutionSettings, GetSuccessfulTestCases());
    }

    public IClassExecutionSummary FilterForFailureTestCases()
    {
        return new CombinedClassExecutionSummary(TestClass, ExecutionSettings, GetFailedTestCases());
    }
}