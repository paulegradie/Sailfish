using System;
using System.Collections.Generic;
using Sailfish.ExtensionMethods;
using Sailfish.Statistics;

namespace Sailfish.Execution;

internal class ExecutionSummary
{
    public ExecutionSummary(Type type, List<CompiledResult> compiledResults)
    {
        Type = type;
        CompiledResults = compiledResults;
        Settings = type.RetrieveExecutionTestSettings();
    }

    public Type Type { get; set; }
    public ExecutionSettings Settings { get; }
    public List<CompiledResult> CompiledResults { get; set; }
}