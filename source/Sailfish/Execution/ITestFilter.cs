using System;

namespace Sailfish.Execution
{
    public interface ITestFilter
    {
        TestValidationResult FilterAndValidate(Type[] tests, string[] testsRequestedByUser);
    }
}