using System;

namespace PerformanceTests;

public class TestException : Exception
{
    public TestException(string? message) : base(message)
    {
    }
}