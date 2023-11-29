using System;
using System.Text.Json.Serialization;
using Sailfish.Analysis;
using Sailfish.Contracts.Serialization.V1.JsonConverters;

#pragma warning disable CS8618

namespace Sailfish.Contracts.Serialization.V1;

/// <summary>
/// Data structure contract used specifically for serializing and deserializing tracking file data
/// Changes to this constitute a **BREAKING CHANGE** in the Sailfish data persistence contract
/// Do not make changes to this lightly
/// </summary>
public class CompiledTestCaseResultTrackingFormat
{
    [JsonConstructor]
    public CompiledTestCaseResultTrackingFormat()
    {
    }

    public CompiledTestCaseResultTrackingFormat(
        string? groupingId,
        PerformanceRunResultTrackingFormat? performanceRunResult,
        Exception? exception,
        TestCaseId? testCaseId)
    {
        GroupingId = groupingId;
        PerformanceRunResult = performanceRunResult;
        Exception = exception;
        TestCaseId = testCaseId;
    }

    public string? GroupingId { get; set; }
    public PerformanceRunResultTrackingFormat? PerformanceRunResult { get; set; }
    public Exception? Exception { get; set; }

    [JsonConverter(typeof(TestCaseIdConverter))]
    public TestCaseId? TestCaseId { get; set; }
}