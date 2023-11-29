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

    public TestCaseResults(TestCaseId testCaseId, TestResultWithOutlierAnalysis testResultsWithOutlierAnalysis)
    {
        TestResultsWithOutlierAnalysis = testResultsWithOutlierAnalysis;
        TestCaseId = testCaseId;
    }

    public TestCaseId TestCaseId { get; set; }
    public TestResultWithOutlierAnalysis TestResultsWithOutlierAnalysis { get; }
}