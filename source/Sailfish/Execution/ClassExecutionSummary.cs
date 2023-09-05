using System;
using System.Collections.Generic;
using Sailfish.Extensions.Methods;
using Sailfish.Statistics;

namespace Sailfish.Execution;

public interface IClassExecutionSummary
{
    public Type Type { get; set; }
    public IExecutionSettings Settings { get; }
    public IEnumerable<ICompiledTestCaseResult> CompiledTestCaseResults { get; set; }
}

public class ClassExecutionSummary : IClassExecutionSummary
{
    public ClassExecutionSummary(Type type, IEnumerable<ICompiledTestCaseResult> compiledResults)
    {
        Type = type;
        CompiledTestCaseResults = compiledResults;
        Settings = type.RetrieveExecutionTestSettings();
    }

    public Type Type { get; set; }
    public IExecutionSettings Settings { get; }
    public IEnumerable<ICompiledTestCaseResult> CompiledTestCaseResults { get; set; }
}