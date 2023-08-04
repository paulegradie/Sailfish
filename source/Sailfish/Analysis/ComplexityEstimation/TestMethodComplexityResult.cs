using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sailfish.Analysis.ComplexityEstimation;

public interface ITestMethodComplexityResult
{
    string TestMethodName { get; set; }
    IEnumerable<ITestPropertyComplexityResult> TestPropertyComplexityResults { get; set; }
}

internal class TestMethodComplexityResult : ITestMethodComplexityResult
{
    [JsonConstructor]
    public TestMethodComplexityResult()
    {
    }

    private TestMethodComplexityResult(string testMethodName, IEnumerable<ITestPropertyComplexityResult> testPropertyComplexityResults)
    {
        TestMethodName = testMethodName;
        TestPropertyComplexityResults = testPropertyComplexityResults;
    }

    public string TestMethodName { get; set; }
    public IEnumerable<ITestPropertyComplexityResult> TestPropertyComplexityResults { get; set; }

    public static IEnumerable<TestMethodComplexityResult> ParseResult(IEnumerable<KeyValuePair<string, Dictionary<string, ComplexityResult>>> rawResult)
    {
        return rawResult
            .Select(
                x =>
                    new TestMethodComplexityResult(x.Key, TestPropertyComplexityResult.ParseResult(x.Value)));
    }
}