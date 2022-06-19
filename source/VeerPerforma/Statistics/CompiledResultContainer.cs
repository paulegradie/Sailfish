using System;
using System.Collections.Generic;

namespace VeerPerforma.Statistics;

public class CompiledResultContainer
{
    public CompiledResultContainer(List<Exception> exceptions, Type type, List<CompiledResult> compiledResults)
    {
        Exceptions = exceptions;
        Type = type;
        CompiledResults = compiledResults;
    }

    public Type Type { get; set; }
    public int StatusCode { get; set; }
    public ExecutionSettings Settings => Type.GetExecutionSettings();
    public List<CompiledResult> CompiledResults { get; set; }
    public List<Exception> Exceptions { get; set; }

}