using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Public.Models;

/// <summary>
/// Represents a group of test cases that should be compared against each other.
/// </summary>
public class TestCaseComparisonGroup
{
    [JsonConstructor]
    public TestCaseComparisonGroup()
    {
        TestCases = new List<TestCaseId>();
    }

    public TestCaseComparisonGroup(string groupName, IEnumerable<TestCaseId> testCases, string? baselineMethod = null)
    {
        GroupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
        TestCases = testCases?.ToList() ?? throw new ArgumentNullException(nameof(testCases));
        BaselineMethod = baselineMethod;
    }

    public string GroupName { get; init; } = string.Empty;
    public List<TestCaseId> TestCases { get; init; }
    public string? BaselineMethod { get; init; }
    public double SignificanceLevel { get; init; } = 0.05;

    [JsonIgnore]
    public string DisplayName => $"Comparison Group: {GroupName}";

    public bool HasBaseline => !string.IsNullOrEmpty(BaselineMethod);

    public TestCaseId? GetBaselineTestCase()
    {
        if (!HasBaseline) return null;
        return TestCases.FirstOrDefault(tc => tc.TestCaseName.GetMethodPart().Equals(BaselineMethod, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<TestCaseId> GetNonBaselineTestCases()
    {
        if (!HasBaseline) return TestCases;
        return TestCases.Where(tc => !tc.TestCaseName.GetMethodPart().Equals(BaselineMethod, StringComparison.OrdinalIgnoreCase));
    }
}
