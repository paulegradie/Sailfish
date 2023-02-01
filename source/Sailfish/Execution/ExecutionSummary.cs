using System;
using System.Collections.Generic;
using Sailfish.ExtensionMethods;
using Sailfish.Statistics;

namespace Sailfish.Execution;

public interface IExecutionSummary
{
    public Type Type { get; set; }
    public IExecutionSettings Settings { get; }
    public List<ICompiledResult> CompiledResults { get; set; }
}

internal class ExecutionSummary : IExecutionSummary
{
    public ExecutionSummary(Type type, List<ICompiledResult> compiledResults)
    {
        Type = type;
        CompiledResults = compiledResults;
        Settings = type.RetrieveExecutionTestSettings();
    }

    public Type Type { get; set; }
    public IExecutionSettings Settings { get; }
    public List<ICompiledResult> CompiledResults { get; set; }
}