using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Execution;

internal class TestFilter : ITestFilter
{
    private readonly ITestListValidator testListValidator;

    public TestFilter(ITestListValidator testListValidator)
    {
        this.testListValidator = testListValidator;
    }

    public TestInitializationResult FilterAndValidate(IEnumerable<Type> tests, IEnumerable<string> testsRequestedByUser)
    {
        var requestedByUser = testsRequestedByUser.ToList();
        var filtered = FilterTests(tests, requestedByUser);
        var result = testListValidator.ValidateTests(requestedByUser, filtered);
        return result;
    }

    private static IEnumerable<Type> FilterTests(IEnumerable<Type> tests, IEnumerable<string> testsRequestedByUser)
    {
        var requestedByUser = testsRequestedByUser as string[] ?? testsRequestedByUser.ToArray();
        if (requestedByUser.Any())
            return tests
                .Where(test => requestedByUser.Contains(test.Name))
                .ToArray();

        return tests;
    }
}