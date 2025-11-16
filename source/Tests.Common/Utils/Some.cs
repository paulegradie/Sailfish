using Sailfish.Contracts.Public.Models;

namespace Tests.Common.Utils;

public static class Some
{
    public static string RandomString()
    {
        return Guid.NewGuid().ToString();
    }

    public static TestCaseId SimpleTestCaseId()
    {
        return new TestCaseId($"{RandomString()}.{RandomString()}()");
    }
}