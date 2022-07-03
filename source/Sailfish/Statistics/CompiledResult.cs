using System;

namespace Sailfish.Statistics;

internal class CompiledResult
{
    public CompiledResult(string displayName, string groupingId, TestCaseStatistics testCaseStatistics)
    {
        DisplayName = displayName;
        GroupingId = groupingId;
        TestCaseStatistics = testCaseStatistics;
    }

    public CompiledResult(Exception exception)
    {
        Exception = exception;
    }

    public string? GroupingId { get; set; }
    public TestCaseStatistics? TestCaseStatistics { get; set; }
    public Exception? Exception { get; set; }
    public string? DisplayName { get; set; }
    
}