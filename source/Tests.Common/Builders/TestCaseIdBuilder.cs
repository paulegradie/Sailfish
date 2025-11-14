using Sailfish.Contracts.Public.Models;
using System.Collections.Generic;
using Tests.Common.Utils;

namespace Tests.Common.Builders;

public class TestCaseIdBuilder
{
    private TestCaseName? _testCaseName;
    private TestCaseVariables? _testCaseVariables;

    public static TestCaseIdBuilder Create() => new();

    public TestCaseIdBuilder WithTestCaseName(string displayName)
    {
        _testCaseName = new TestCaseName(displayName);
        return this;
    }

    public TestCaseIdBuilder WithTestCaseName(string name, IReadOnlyList<string> parts)
    {
        _testCaseName = new TestCaseName(name, parts);
        return this;
    }

    public TestCaseIdBuilder WithTestCaseVariables(IEnumerable<TestCaseVariable> variables)
    {
        _testCaseVariables = new TestCaseVariables(variables);
        return this;
    }

    public TestCaseId Build()
    {
        return new TestCaseId(
            _testCaseName ?? new TestCaseName(nameof(ClassExecutionSummaryTrackingFormatBuilder.TestClass)),
            _testCaseVariables ?? new TestCaseVariables(new List<TestCaseVariable>
            {
                new(Some.RandomString(), 5)
            }));
    }
}