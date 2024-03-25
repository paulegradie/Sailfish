using System.Collections.Generic;
using System.Linq;
using Sailfish.Contracts.Public.Models;

namespace Sailfish.Extensions.Methods;

public static class TestCaseResultOrderingExtensionMethods
{
    public static List<SailDiffResult> OrderByTestCaseId(this IEnumerable<SailDiffResult> testCaseResults)
    {
        return [.. testCaseResults.OrderBy(x => x.TestCaseId, new TestCaseIdComparer())];
    }
}