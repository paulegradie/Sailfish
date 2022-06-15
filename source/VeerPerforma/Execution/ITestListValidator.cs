using System;

namespace VeerPerforma.Execution
{
    public interface ITestListValidator
    {
        TestValidationResult ValidateTests(string[] testsRequestedByUser, Type[] filteredTestNames);
    }
}