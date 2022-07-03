using System;

namespace Sailfish.Statistics;

internal class CompiledResult
{
    private CompiledResult(string displayName, string groupingId, TestCaseStatistics testCaseStatistics)
    {
        DisplayName = displayName;
        GroupingId = groupingId;
        TestCaseStatistics = testCaseStatistics;
    }

    public CompiledResult(Exception exception)
    {
        Exception = exception;
    }

    public string GroupingId { get; set; }
    public TestCaseStatistics TestCaseStatistics { get; set; }
    public Exception? Exception { get; set; }
    public string DisplayName { get; set; }

    public static CompiledResult CreateSuccessfulCompiledResult(string displayName, string groupingId, TestCaseStatistics testCaseStatistics)
    {
        return new CompiledResult(displayName, groupingId, testCaseStatistics);
    }

    public static CompiledResult CreateFailedCompiledResult(Exception exception)
    {
        return new CompiledResult(exception);
    }
}