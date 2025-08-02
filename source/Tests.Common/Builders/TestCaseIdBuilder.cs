using Sailfish.Contracts.Public.Models;
using System.Collections.Generic;
using Tests.Common.Utils;

namespace Tests.Common.Builders;

public class TestCaseIdBuilder
{
    private TestCaseName? testCaseName;
    private TestCaseVariables? testCaseVariables;

    public static TestCaseIdBuilder Create() => new();

    public TestCaseIdBuilder WithTestCaseName(string displayName)
    {
        testCaseName = new TestCaseName(displayName);
        return this;
    }

    public TestCaseIdBuilder WithTestCaseName(string name, IReadOnlyList<string> parts)
    {
        testCaseName = new TestCaseName(name, parts);
        return this;
    }

    public TestCaseIdBuilder WithTestCaseVariables(IEnumerable<TestCaseVariable> variables)
    {
        testCaseVariables = new TestCaseVariables(variables);
        return this;
    }

    public TestCaseId Build()
    {
        return new TestCaseId(
            testCaseName ?? new TestCaseName(nameof(ClassExecutionSummaryTrackingFormatBuilder.TestClass)),
            testCaseVariables ?? new TestCaseVariables(new List<TestCaseVariable>
            {
                new(Some.RandomString(), 5)
            }));
    }
}