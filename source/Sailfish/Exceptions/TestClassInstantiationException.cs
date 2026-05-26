using System;

namespace Sailfish.Exceptions;

public class TestClassInstantiationException : Exception
{
    public TestClassInstantiationException(Type testType, Exception inner, string? message = null)
        : base(message ?? $"Failed to instantiate test class '{testType.FullName}'. " +
                          "This usually indicates a missing dependency registration or a constructor that threw.", inner)
    {
        TestType = testType;
    }

    public Type TestType { get; }
}
