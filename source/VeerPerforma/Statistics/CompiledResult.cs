using System;
using VeerPerforma.Attributes;
using VeerPerforma.Utils;

namespace VeerPerforma.Statistics;

public class CompiledResult
{
    public CompiledResult(string displayName, string groupingId, TestCaseStatistics testCaseStatistics)
    {
        DisplayName = displayName;
        GroupingId = groupingId;
        TestCaseStatistics = testCaseStatistics;
    }

    public string GroupingId { get; set; }
    public TestCaseStatistics TestCaseStatistics { get; set; }
    public Exception? Exception { get; set; }
    public string DisplayName { get; set; }
}