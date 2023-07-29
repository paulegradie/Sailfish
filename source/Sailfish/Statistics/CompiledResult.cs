using System;
using System.Collections.Generic;
using Sailfish.Analysis;
using Sailfish.Contracts.Public;

namespace Sailfish.Statistics;

public interface ICompiledTestCaseResult
{
    public string? GroupingId { get; set; }
    public DescriptiveStatisticsResult? DescriptiveStatisticsResult { get; set; }
    public List<Exception> Exceptions { get; set; }
    public TestCaseId? TestCaseId { get; set; }
}

internal class CompiledTestCaseResult : ICompiledTestCaseResult
{
    public CompiledTestCaseResult(TestCaseId testCaseId, string groupingId, DescriptiveStatisticsResult descriptiveStatisticsResult)
    {
        TestCaseId = testCaseId;
        GroupingId = groupingId;
        DescriptiveStatisticsResult = descriptiveStatisticsResult;
    }

    public CompiledTestCaseResult(Exception exception) : this(new List<Exception>() { exception })
    {
    }

    public CompiledTestCaseResult(IEnumerable<Exception> exceptions)
    {
        Exceptions.AddRange(exceptions);
    }

    public string? GroupingId { get; set; }
    public DescriptiveStatisticsResult? DescriptiveStatisticsResult { get; set; }
    public List<Exception> Exceptions { get; set; } = new();
    public TestCaseId? TestCaseId { get; set; }
}