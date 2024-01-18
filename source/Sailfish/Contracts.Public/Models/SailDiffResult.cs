using Sailfish.Analysis.SailDiff.Statistics.Tests;
using System.Text.Json.Serialization;

#pragma warning disable CS8618

namespace Sailfish.Contracts.Public.Models;

public class SailDiffResult
{
    [JsonConstructor]
    public SailDiffResult()
    {
    }

    public SailDiffResult(TestCaseId testCaseId, TestResultWithOutlierAnalysis testResultsWithOutlierAnalysis)
    {
        TestResultsWithOutlierAnalysis = testResultsWithOutlierAnalysis;
        TestCaseId = testCaseId;
    }

    public TestCaseId TestCaseId { get; set; }
    public TestResultWithOutlierAnalysis TestResultsWithOutlierAnalysis { get; }
}