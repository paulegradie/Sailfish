using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Execution;

public class SailfishValidity
{
    private SailfishValidity(bool isValid, IEnumerable<Exception>? exceptions = null)
    {
        IsValid = isValid;
        Exceptions = exceptions ?? Enumerable.Empty<Exception>();
    }

    public bool IsValid { get; }
    public IEnumerable<Exception>? Exceptions { get; }

    public static SailfishValidity CreateValidResult()
    {
        return new SailfishValidity(true);
    }

    public static SailfishValidity CreateInvalidResult(IEnumerable<Exception> exceptions)
    {
        return new SailfishValidity(false, exceptions);
    }
}