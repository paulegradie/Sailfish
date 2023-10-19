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

    public static SailfishRunResult CreateResult(IEnumerable<IClassExecutionSummary> executionSummaries, List<Exception> exceptions)
    {
        return new SailfishRunResult(!exceptions.Any(), executionSummaries, exceptions);
    }
}