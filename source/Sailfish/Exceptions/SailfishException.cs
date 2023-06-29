using System;

namespace Sailfish.Exceptions;

public class SailfishException : Exception
{
    public SailfishException(string? message = null) : base(message)
    {
    }

    public SailfishException(Exception ex) : base(ex.Message)
    {
    }
}