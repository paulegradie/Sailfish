using System;

namespace Sailfish.Statistics;

internal class CompiledResult
{
    public CompiledResult(string displayName, string groupingId, DescriptiveStatistics descriptiveStatistics)
    {
        DisplayName = displayName;
        GroupingId = groupingId;
        DescriptiveStatistics = descriptiveStatistics;
    }

    public CompiledResult(Exception exception)
    {
        Exception = exception;
    }

    public string? GroupingId { get; set; }
    public DescriptiveStatistics? DescriptiveStatistics { get; set; }
    public Exception? Exception { get; set; }
    public string? DisplayName { get; set; }
    
}