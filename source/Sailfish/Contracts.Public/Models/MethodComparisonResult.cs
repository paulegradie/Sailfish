using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Public.Models;

/// <summary>
/// Represents the result of comparing multiple methods within the same test run.
/// </summary>
public class MethodComparisonResult
{
    [JsonConstructor]
    public MethodComparisonResult()
    {
        ComparisonGroup = new TestCaseComparisonGroup();
        PairwiseComparisons = new List<SailDiffResult>();
        MethodRankings = new List<MethodRanking>();
    }

    public MethodComparisonResult(
        TestCaseComparisonGroup comparisonGroup,
        IEnumerable<SailDiffResult> pairwiseComparisons,
        IEnumerable<MethodRanking> methodRankings)
    {
        ComparisonGroup = comparisonGroup ?? throw new ArgumentNullException(nameof(comparisonGroup));
        PairwiseComparisons = pairwiseComparisons?.ToList() ?? throw new ArgumentNullException(nameof(pairwiseComparisons));
        MethodRankings = methodRankings?.ToList() ?? throw new ArgumentNullException(nameof(methodRankings));
    }

    public TestCaseComparisonGroup ComparisonGroup { get; init; }
    public List<SailDiffResult> PairwiseComparisons { get; init; }
    public List<MethodRanking> MethodRankings { get; init; }
    public DateTime ComparisonTimestamp { get; init; } = DateTime.UtcNow;

    [JsonIgnore]
    public string DisplayName => ComparisonGroup.DisplayName;

    public MethodRanking? GetFastestMethod()
    {
        return MethodRankings.OrderBy(r => r.Rank).FirstOrDefault();
    }

    public MethodRanking? GetSlowestMethod()
    {
        return MethodRankings.OrderByDescending(r => r.Rank).FirstOrDefault();
    }
}

/// <summary>
/// Represents the ranking of a method within a comparison group.
/// </summary>
public class MethodRanking
{
    public MethodRanking(TestCaseId testCaseId, int rank, double medianExecutionTime, double relativePerformance)
    {
        TestCaseId = testCaseId ?? throw new ArgumentNullException(nameof(testCaseId));
        Rank = rank;
        MedianExecutionTime = medianExecutionTime;
        RelativePerformance = relativePerformance;
    }

    public TestCaseId TestCaseId { get; init; }
    public int Rank { get; init; }
    public double MedianExecutionTime { get; init; }
    public double RelativePerformance { get; init; } // 1.0 = baseline, >1.0 = slower, <1.0 = faster
    public string MethodName => TestCaseId.TestCaseName.GetMethodPart();
}
