using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sailfish.Analysis.ComplexityEstimation;

public interface ITestClassComplexityResult
{
    string TestClassName { get; set; }
    IEnumerable<ITestMethodComplexityResult> TestMethodComplexityResults { get; set; }
}

internal class TestClassComplexityResult : ITestClassComplexityResult
{
    [JsonConstructor]
    public TestClassComplexityResult()
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