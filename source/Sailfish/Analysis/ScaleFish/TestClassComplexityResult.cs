using System;
using System.Collections.Generic;
using System.Linq;

namespace Sailfish.Analysis.ScaleFish;

public interface ITestClassComplexityResult
{
    string TestClassName { get; set; }
    IEnumerable<TestMethodComplexityResult> TestMethodComplexityResults { get; set; }
}

public class TestClassComplexityResult : ITestClassComplexityResult
{
    public TestClassComplexityResult(string testClassName, IEnumerable<TestMethodComplexityResult> testMethodComplexityResults)
    {
        TestClassName = testClassName;
        TestMethodComplexityResults = testMethodComplexityResults;
    }

    public string TestClassName { get; set; }
    public IEnumerable<TestMethodComplexityResult> TestMethodComplexityResults { get; set; }

    public static IEnumerable<ITestClassComplexityResult> ParseResults(Dictionary<Type, Dictionary<string, Dictionary<string, ComplexityResult>>> rawResult)
    {
        return rawResult
            .Select(
                x =>
                    new TestClassComplexityResult(x.Key.Name, TestMethodComplexityResult.ParseResult(x.Value)));
    }
}