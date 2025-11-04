using Sailfish.Contracts.Public.Models;
using System;

namespace Sailfish.TestAdapter.Queue.Processors.MethodComparison;

/// <summary>
/// A wrapper for ICompiledTestCaseResult that allows modifying the PerformanceRunResult.
/// Used for method comparisons where we need to use a common test case ID.
/// </summary>
internal class ModifiedCompiledTestCaseResult : ICompiledTestCaseResult
{
    private readonly ICompiledTestCaseResult _original;
    private readonly PerformanceRunResult _modifiedPerformanceResult;

    public ModifiedCompiledTestCaseResult(ICompiledTestCaseResult original, PerformanceRunResult modifiedPerformanceResult)
    {
        _original = original;
        _modifiedPerformanceResult = modifiedPerformanceResult;
    }

    public TestCaseId? TestCaseId => _original.TestCaseId;
    public string? GroupingId => _original.GroupingId;
    public PerformanceRunResult? PerformanceRunResult => _modifiedPerformanceResult;
    public Exception? Exception => _original.Exception;
}