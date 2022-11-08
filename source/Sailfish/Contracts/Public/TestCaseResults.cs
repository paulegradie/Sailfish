using System.Text.Json.Serialization;
using Sailfish.Statistics.Tests;

#pragma warning disable CS8618

namespace Sailfish.Contracts.Public;

public class TestCaseResults
{
    [JsonConstructor]
    public TestCaseResults()
    {
    }

    public TestCaseResults(string displayName, TestResults testResults)
    {
        TestResults = testResults;
        DisplayName = displayName;
    }

    public string DisplayName { get; set; }
    public TestResults TestResults { get; }
}