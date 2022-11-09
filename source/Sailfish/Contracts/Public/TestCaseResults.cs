using System.Text.Json.Serialization;
using Sailfish.Analysis;
using Sailfish.Statistics.Tests;

#pragma warning disable CS8618

namespace Sailfish.Contracts.Public;

public class TestCaseResults
{
    [JsonConstructor]
    public TestCaseResults()
    {
    }

    public TestCaseResults(TestCaseId TestCaseId, TestResults testResults)
    {
        TestResults = testResults;
        this.TestCaseId = TestCaseId;
    }

    public TestCaseId TestCaseId { get; set; }
    public TestResults TestResults { get; }
}