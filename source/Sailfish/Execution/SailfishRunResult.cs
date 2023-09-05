using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Execution;

public class SailfishRunResult
{
    private SailfishRunResult(bool isValid, IEnumerable<IClassExecutionSummary> executionSummaries, IEnumerable<Exception>? exceptions = null)
    {
        IsValid = isValid;
        Exceptions = exceptions ?? Enumerable.Empty<Exception>();
        ExecutionSummaries = executionSummaries;
    }

    public bool IsValid { get; }
    public IEnumerable<Exception>? Exceptions { get; }
    public IEnumerable<IClassExecutionSummary> ExecutionSummaries { get; set; }

    public static SailfishRunResult CreateValidResult(IEnumerable<IClassExecutionSummary> executionSummaries)
    {
        return new SailfishRunResult(true, executionSummaries);
    }

    public static SailfishRunResult CreateInvalidResult(IEnumerable<Exception> exceptions)
    {
        return new SailfishRunResult(false, new List<ClassExecutionSummary>(), exceptions);
    }
}