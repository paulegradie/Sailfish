using System;
using System.Text.Json.Serialization;

namespace Sailfish.Analysis;

/// <summary>
/// Strong Typing for Test Case strings
/// </summary>
public class TestCaseId
{
    [JsonConstructor]
    public TestCaseId()
    {
    }

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

    public TestCaseName TestCaseName { get; init; } = null!;
    public TestCaseVariables TestCaseVariables { get; init; } = null!;

    [JsonIgnore] public string DisplayName => FormDisplayName();

    public string GetMethodWithVariables()
    {
        return TestCaseName.GetMethodPart() + TestCaseVariables.FormVariableSection();
    }

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