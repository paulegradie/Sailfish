using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
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
        return requestedByUser.Any()
            ? tests.Where(test => requestedByUser.Any(requested =>
            {
                var name = test.FullName ?? test.Name;
                return name.EndsWith(requested);
            }))
            : tests;
    }
}