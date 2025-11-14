using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Execution;

internal interface ITestFilter
{
    TestInitializationResult FilterAndValidate(IEnumerable<Type> tests, IEnumerable<string> testsRequestedByUser);
}

internal class TestFilter(ITestListValidator testListValidator) : ITestFilter
{
    private readonly ITestListValidator _testListValidator = testListValidator;

    public TestInitializationResult FilterAndValidate(IEnumerable<Type> tests, IEnumerable<string> testsRequestedByUser)
    {
        var requestedByUser = testsRequestedByUser.ToList();
        var filtered = FilterTests(tests, requestedByUser);
        var result = _testListValidator.ValidateTests(requestedByUser, filtered);
        return result;
    }

    private static IEnumerable<Type> FilterTests(IEnumerable<Type> tests, IEnumerable<string> testsRequestedByUser)
    {
        var requestedByUser = testsRequestedByUser as string[] ?? testsRequestedByUser.ToArray();
        return requestedByUser.Any()
            ? tests.Where(test => requestedByUser.Any(requested =>
            {
                var name = test.FullName ?? test.Name;
                return name.EndsWith(requested);
            }))
            : tests;
    }
}