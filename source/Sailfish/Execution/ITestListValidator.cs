using System;
using System.Collections.Generic;

namespace Sailfish.Execution;

internal interface ITestListValidator
{
    TestInitializationResult ValidateTests(IEnumerable<string> testsRequestedByUser, IEnumerable<Type> filteredTestNames);
}