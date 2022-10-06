using System;
using Sailfish.Contracts.Public;

namespace Sailfish.Statistics;

internal class CompiledResult
{
    public CompiledResult(string displayName, string groupingId, DescriptiveStatisticsResult descriptiveStatisticsResult)
    {
        DisplayName = displayName;
        GroupingId = groupingId;
        DescriptiveStatisticsResult = descriptiveStatisticsResult;
    }

    public CompiledResult(Exception exception)
    {
        Exception = exception;
    }

    public string? GroupingId { get; set; }
    public DescriptiveStatisticsResult? DescriptiveStatisticsResult { get; set; }
    public Exception? Exception { get; set; }
    public string? DisplayName { get; set; }
    
}