using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sailfish.Analysis.Scalefish;

public interface ITestPropertyComplexityResult
{
    public string MethodName { get; set; }
    string PropertyName { get; set; }
    ComplexityResult ComplexityResult { get; set; }
}

public class TestPropertyComplexityResult : ITestPropertyComplexityResult
{
    [JsonConstructor]
#pragma warning disable CS8618
    public TestPropertyComplexityResult()
#pragma warning restore CS8618
    {
    }

    private TestPropertyComplexityResult(string propertyName, ComplexityResult complexityResult)
    {
        var nameParts = propertyName.Split("-");
        MethodName = nameParts[0];
        PropertyName = nameParts[1];
        ComplexityResult = complexityResult;
    }

    public string MethodName { get; set; }
    public string PropertyName { get; set; }
    public ComplexityResult ComplexityResult { get; set; }

    public static IEnumerable<TestPropertyComplexityResult> ParseResult(Dictionary<string, ComplexityResult> rawResult)
    {
        return rawResult.Select(x => new TestPropertyComplexityResult(x.Key, x.Value));
    }
}