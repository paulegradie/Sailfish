using System;

namespace Sailfish.Execution
{
    public interface ITestListValidator
    {
        TestValidationResult ValidateTests(string[] testsRequestedByUser, Type[] filteredTestNames);
    }
}