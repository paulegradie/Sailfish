using System;
using Sailfish.Attributes;
using Sailfish.Utils;

namespace Sailfish.Statistics;

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