using System;
using System.Text.Json.Serialization;

namespace Sailfish.Analysis;

public class TestCaseId
{
    [JsonConstructor]
    public TestCaseId(string displayName)
    {
        TestCaseName = new TestCaseName(displayName);
        TestCaseVariables = new TestCaseVariables(displayName);
    }

    public TestCaseId(TestCaseName testCaseName, TestCaseVariables testCaseVariables)
    {
        TestCaseName = testCaseName;
        TestCaseVariables = testCaseVariables;
    }

    public TestCaseName TestCaseName { get; }
    public TestCaseVariables TestCaseVariables { get; }

    public string DisplayName => FormDisplayName();

    private string FormDisplayName()
    {
        return TestCaseName.Name + TestCaseVariables.FormVariableSection();
    }

    public override bool Equals(object? other)
    {
        return other is TestCaseId id && DisplayName.Equals(id.DisplayName, StringComparison.InvariantCultureIgnoreCase);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TestCaseName, TestCaseVariables);
    }
}