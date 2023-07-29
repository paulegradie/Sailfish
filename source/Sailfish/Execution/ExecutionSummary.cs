using System;
using System.Collections.Generic;
using Sailfish.Extensions.Methods;
using Sailfish.Statistics;

namespace Sailfish.Execution;

public interface IExecutionSummary
{
    public Type Type { get; set; }
    public IExecutionSettings Settings { get; }
    public List<ICompiledTestCaseResult> CompiledTestCaseResults { get; set; }
}

internal class ExecutionSummary : IExecutionSummary
{
    public ExecutionSummary(Type type, List<ICompiledTestCaseResult> compiledResults)
    {
        Type = type;
        CompiledTestCaseResults = compiledResults;
        Settings = type.RetrieveExecutionTestSettings();
    }

    public Type Type { get; set; }
    public IExecutionSettings Settings { get; }
    public List<ICompiledTestCaseResult> CompiledTestCaseResults { get; set; }
}