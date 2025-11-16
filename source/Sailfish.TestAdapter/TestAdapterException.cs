using System;

namespace Sailfish.TestAdapter;

public class TestAdapterException : Exception
{
    public TestAdapterException(string? message = null, Exception? exception = null) : base(message, exception)
    {
    }
}