using System;

namespace Sailfish.Exceptions;

public class SailfishModelException : Exception
{
    public SailfishModelException(string? message = null) : base(message)
    {
    }

    public SailfishModelException(Exception ex) : base(ex.Message)
    {
    }
}