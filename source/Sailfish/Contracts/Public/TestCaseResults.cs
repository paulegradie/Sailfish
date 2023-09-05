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

    public TestCaseResults(TestCaseId TestCaseId, TestResultWithOutlierAnalysis testResultsWithOutlierAnalysis)
    {
        TestResultsWithOutlierAnalysis = testResultsWithOutlierAnalysis;
        this.TestCaseId = TestCaseId;
    }

    public TestCaseId TestCaseId { get; set; }
    public TestResultWithOutlierAnalysis TestResultsWithOutlierAnalysis { get; }
}