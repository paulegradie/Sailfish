using System;
using Sailfish.Analysis;
using Sailfish.Contracts.Public;

namespace Sailfish.Statistics;

public interface ICompiledResult
{
    public string? GroupingId { get; set; }
    public DescriptiveStatisticsResult? DescriptiveStatisticsResult { get; set; }
    public Exception? Exception { get; set; }
    public TestCaseId? TestCaseId { get; set; }
}

internal class CompiledResult : ICompiledResult
{
    public CompiledResult(TestCaseId testCaseId, string groupingId, DescriptiveStatisticsResult descriptiveStatisticsResult)
    {
        TestCaseId = testCaseId;
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
    public TestCaseId? TestCaseId { get; set; }
}