using System;
using System.Collections.Generic;
using System.Linq;
using Sailfish.Extensions.Methods;
using Sailfish.Statistics;

namespace Sailfish.Execution;

public interface IClassExecutionSummary
{
    public Type TestClass { get; set; }
    public IExecutionSettings Settings { get; }
    public IEnumerable<ICompiledTestCaseResult> CompiledTestCaseResults { get; set; }

    public IEnumerable<ICompiledTestCaseResult> GetSuccessfulTestCases();
    IEnumerable<ICompiledTestCaseResult> GetFailedTestCases();
}

public class ClassExecutionSummary : IClassExecutionSummary
{
    public ClassExecutionSummary(Type type, IEnumerable<ICompiledTestCaseResult> compiledResults)
    {
        TestClass = type;
        CompiledTestCaseResults = compiledResults;
        Settings = type.RetrieveExecutionTestSettings();
    }

    public Type TestClass { get; set; }
    public IExecutionSettings Settings { get; }
    public IEnumerable<ICompiledTestCaseResult> CompiledTestCaseResults { get; set; }

    public IEnumerable<ICompiledTestCaseResult> GetSuccessfulTestCases()
    {
        return CompiledTestCaseResults.Where(x => x.PerformanceRunResult is not null);
    }

    public IEnumerable<ICompiledTestCaseResult> GetFailedTestCases()
    {
        return CompiledTestCaseResults.Where(x => x.PerformanceRunResult is null);
    }
}