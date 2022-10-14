using System;

namespace Sailfish.Execution;

internal interface ITestFilter
{
    TestValidationResult FilterAndValidate(Type[] tests, string[] testsRequestedByUser);
}