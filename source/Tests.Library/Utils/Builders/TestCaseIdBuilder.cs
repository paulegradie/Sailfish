using Sailfish.Contracts.Public.Models;
using System;
using System.Collections.Generic;

namespace Tests.Library.Utils.Builders;

public class TestCaseIdBuilder
{
    private TestCaseName? testCaseName;
    private TestCaseVariables? testCaseVariables;

    public static TestCaseIdBuilder Create() => new();

    public TestCaseIdBuilder WithTestCaseName(string displayName)
    {
        this.testCaseName = new TestCaseName(displayName);
        return this;
    }

    public TestCaseIdBuilder WithTestCaseName(string name, IReadOnlyList<string> parts)
    {
        this.testCaseName = new TestCaseName(name, parts);
        return this;
    }

    public TestCaseIdBuilder WithTestCaseVariables(string displayName)
    {
        this.testCaseVariables = new TestCaseVariables(displayName);
        return this;
    }

    public TestCaseIdBuilder WithTestCaseVariables(IEnumerable<TestCaseVariable> variables)
    {
        this.testCaseVariables = new TestCaseVariables(variables);
        return this;
    }

    public TestCaseId Build()
    {
        return new TestCaseId(
            testCaseName ?? new TestCaseName(nameof(ClassExecutionSummaryTrackingFormatBuilder.TestClass)),
            testCaseVariables ?? new TestCaseVariables(new List<TestCaseVariable>()
            {
                new(Some.RandomString(), 5)
            }));
    }
}