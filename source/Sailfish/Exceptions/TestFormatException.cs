using System;

namespace Sailfish.Exceptions;

public class TestFormatException : Exception
{
    public TestFormatException(string? message) : base(message)
    {
    }
}