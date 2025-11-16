using System;

namespace Sailfish.Exceptions;

public class TestCaseEnumerationException : Exception
{
    public TestCaseEnumerationException(Exception ex, string? message) : base(message, ex)
    {
    }
}