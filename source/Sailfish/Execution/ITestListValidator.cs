using System;

namespace Sailfish.Execution;

internal interface ITestListValidator
{
    TestInitializationResult ValidateTests(string[] testsRequestedByUser, Type[] filteredTestNames);
}