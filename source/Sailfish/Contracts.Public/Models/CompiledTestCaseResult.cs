﻿using System;

namespace Sailfish.Contracts.Public.Models;

public interface ICompiledTestCaseResult
{
    public string? GroupingId { get; }
    public PerformanceRunResult? PerformanceRunResult { get; }
    public Exception? Exception { get; }
    public TestCaseId? TestCaseId { get; }
}

internal class CompiledTestCaseResult : ICompiledTestCaseResult
{
    public CompiledTestCaseResult(TestCaseId testCaseId, string groupingId, PerformanceRunResult performanceRunResult)
    {
        TestCaseId = testCaseId;
        GroupingId = groupingId;
        PerformanceRunResult = performanceRunResult;
    }

    public CompiledTestCaseResult(TestCaseId testCaseId, string? groupingId, Exception exception)
    {
        TestCaseId = testCaseId;
        GroupingId = groupingId;
        Exception = exception;
    }

    public string? GroupingId { get; set; }
    public PerformanceRunResult? PerformanceRunResult { get; set; }
    public Exception? Exception { get; set; }
    public TestCaseId TestCaseId { get; set; }
}