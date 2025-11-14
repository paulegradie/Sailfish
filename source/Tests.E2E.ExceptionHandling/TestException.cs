namespace Tests.E2E.ExceptionHandling;

public class TestException : Exception
{
    public TestException(string? message = null) : base(message)
    {
    }
}