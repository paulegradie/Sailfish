using System.Collections.Generic;
using System.Linq;
using Sailfish.Analysis;
using Sailfish.Contracts.Public;

namespace Sailfish.Extensions.Methods;

public static class TestCaseResultOrderingExtensionMethods
{
    public static List<TestCaseResults> OrderByTestCaseId(this IEnumerable<TestCaseResults> testCaseResults)
    {
        return testCaseResults.OrderBy(x => x.TestCaseId, new TestCaseIdComparer()).ToList();
    }
}