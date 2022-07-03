using System;

namespace Sailfish.Execution
{
    internal interface ITestListValidator
    {
        TestValidationResult ValidateTests(string[] testsRequestedByUser, Type[] filteredTestNames);
    }
}