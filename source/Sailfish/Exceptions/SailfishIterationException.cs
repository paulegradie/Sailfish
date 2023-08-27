using System;

namespace Sailfish.Exceptions;

public class SailfishIterationException : Exception
{
    public SailfishIterationException(string? message = null) : base(message)
    {
    }

    public SailfishIterationException(Exception ex) : base(ex.Message)
    {
    }
}