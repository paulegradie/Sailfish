using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.Scalefish;

public interface ITestMethodComplexityResult
{
    string TestMethodName { get; set; }
    IEnumerable<TestPropertyComplexityResult> TestPropertyComplexityResults { get; set; }
}

public class TestMethodComplexityResult : ITestMethodComplexityResult
{
    public TestMethodComplexityResult(string testMethodName, IEnumerable<TestPropertyComplexityResult> testPropertyComplexityResults)
    {
        TestMethodName = testMethodName;
        TestPropertyComplexityResults = testPropertyComplexityResults;
    }

    public string TestMethodName { get; set; }
    public IEnumerable<TestPropertyComplexityResult> TestPropertyComplexityResults { get; set; }

    public static IEnumerable<TestMethodComplexityResult> ParseResult(IEnumerable<KeyValuePair<string, Dictionary<string, ComplexityResult>>> rawResult)
    {
        return rawResult
            .Select(
                x =>
                    new TestMethodComplexityResult(x.Key.Split('.').Last(), TestPropertyComplexityResult.ParseResult(x.Value)));
    }
}