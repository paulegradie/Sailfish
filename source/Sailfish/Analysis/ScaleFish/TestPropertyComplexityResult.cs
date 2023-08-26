using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.Scalefish;

public interface ITestPropertyComplexityResult
{
    string PropertyName { get; set; }
    ComplexityResult ComplexityResult { get; set; }
}

public class TestPropertyComplexityResult : ITestPropertyComplexityResult
{
    public TestPropertyComplexityResult(string propertyName, ComplexityResult complexityResult)
    {
        PropertyName = propertyName;
        ComplexityResult = complexityResult;
    }

    public string PropertyName { get; set; }
    public ComplexityResult ComplexityResult { get; set; }

    public static IEnumerable<TestPropertyComplexityResult> ParseResult(Dictionary<string, ComplexityResult> rawResult)
    {
        return rawResult.Select(x => new TestPropertyComplexityResult(x.Key, x.Value));
    }
}