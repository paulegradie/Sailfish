using System;
using System.Collections.Generic;

namespace Sailfish.Execution;

internal interface ITestFilter
{
    TestInitializationResult FilterAndValidate(IEnumerable<Type> tests, IEnumerable<string> testsRequestedByUser);
}