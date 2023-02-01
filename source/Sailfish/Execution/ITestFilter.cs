using System;

namespace Sailfish.Execution;

internal interface ITestFilter
{
    TestInitializationResult FilterAndValidate(Type[] tests, string[] testsRequestedByUser);
}