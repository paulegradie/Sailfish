using System;

namespace Sailfish.Exceptions;

public class SailfishException : Exception
{
    public SailfishException(string? message) : base(message)
    {
    }
}