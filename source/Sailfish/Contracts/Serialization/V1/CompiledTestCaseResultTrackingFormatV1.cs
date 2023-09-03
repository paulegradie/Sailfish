using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sailfish.Analysis;

#pragma warning disable CS8618

namespace Sailfish.Contracts.Serialization.V1;

/// <summary>
/// Data structure contract used specifically for serializing and deserializing tracking file data
/// Changes to this constitute a **BREAKING CHANGE** in the Sailfish data persistence contract
/// Do not make changes to this lightly
/// </summary>
public class CompiledTestCaseResultTrackingFormatV1
{
    [JsonConstructor]
    public CompiledTestCaseResultTrackingFormatV1()
    {
    }

    public CompiledTestCaseResultTrackingFormatV1(
        string groupingId,
        PerformanceRunResultTrackingFormatV1 performanceRunResultTrackingFormatV1,
        List<Exception> exceptions,
        TestCaseId testCaseId)
    {
        GroupingId = groupingId;
        PerformanceRunResultTrackingFormatV1 = performanceRunResultTrackingFormatV1;
        Exceptions = exceptions;
        TestCaseId = testCaseId;
    }

    public string GroupingId { get; set; }
    public PerformanceRunResultTrackingFormatV1 PerformanceRunResultTrackingFormatV1 { get; set; }
    public List<Exception> Exceptions { get; set; } = new();

    [JsonConverter(typeof(TestCaseIdConverter))]
    public TestCaseId? TestCaseId { get; set; }
}