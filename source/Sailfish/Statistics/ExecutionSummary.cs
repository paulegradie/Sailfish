using System;
using System.Collections.Generic;

namespace Sailfish.Statistics;

internal class ExecutionSummary
{
    public ExecutionSummary(List<Exception> exceptions, Type type, List<CompiledResult> compiledResults, ExecutionSettings settings)
    {
        Exceptions = exceptions;
        Type = type;
        CompiledResults = compiledResults;
        Settings = settings;
    }

    public Type Type { get; set; }
    public int StatusCode { get; set; }
    public ExecutionSettings Settings { get; }
    public List<CompiledResult> CompiledResults { get; set; }
    public List<Exception> Exceptions { get; set; }
}

