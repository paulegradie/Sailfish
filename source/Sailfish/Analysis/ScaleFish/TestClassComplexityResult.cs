using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sailfish.Analysis.Scalefish;

public interface ITestClassComplexityResult
{
    string TestClassName { get; set; }
    IEnumerable<ITestMethodComplexityResult> TestMethodComplexityResults { get; set; }
}

internal class TestClassComplexityResult : ITestClassComplexityResult
{
    [JsonConstructor]
#pragma warning disable CS8618
    public TestClassComplexityResult()
#pragma warning restore CS8618
    {
    }

    private TestClassComplexityResult(string testClassName, IEnumerable<ITestMethodComplexityResult> testMethodComplexityResults)
    {
        TestClassName = testClassName;
        TestMethodComplexityResults = testMethodComplexityResults;
    }

    public string TestClassName { get; set; }
    public IEnumerable<ITestMethodComplexityResult> TestMethodComplexityResults { get; set; }

    public static IEnumerable<ITestClassComplexityResult> ParseResults(Dictionary<Type, Dictionary<string, Dictionary<string, ComplexityResult>>> rawResult)
    {
        return rawResult
            .Select(
                x =>
                    new TestClassComplexityResult(x.Key.Name, TestMethodComplexityResult.ParseResult(x.Value)));
    }
}