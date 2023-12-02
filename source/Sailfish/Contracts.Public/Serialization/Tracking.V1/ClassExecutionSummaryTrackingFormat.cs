using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Sailfish.Contracts.Public.Serialization.JsonConverters;

namespace Sailfish.Contracts.Public.Serialization.Tracking.V1;

/// <summary>
/// Data structure contract used specifically for serializing and deserializing tracking file data
/// Changes to this constitute a **BREAKING CHANGE** in the Sailfish data persistence contract
/// Do not make changes to this lightly
/// </summary>
public class ClassExecutionSummaryTrackingFormat
{
    [JsonConstructor]
#pragma warning disable CS8618
    public ClassExecutionSummaryTrackingFormat()
#pragma warning restore CS8618
    {
    }

    public ClassExecutionSummaryTrackingFormat(
        Type testClass,
        ExecutionSettingsTrackingFormat executionSettings,
        IEnumerable<CompiledTestCaseResultTrackingFormat> compiledTestCaseResults)
    {
        TestClass = testClass;
        ExecutionSettings = executionSettings;
        CompiledTestCaseResults = compiledTestCaseResults;
    }

    [JsonConverter(typeof(TypePropertyConverter))]
    public Type TestClass { get; set; }

    public ExecutionSettingsTrackingFormat ExecutionSettings { get; }
    public IEnumerable<CompiledTestCaseResultTrackingFormat> CompiledTestCaseResults { get; set; }

    public IEnumerable<CompiledTestCaseResultTrackingFormat> GetSuccessfulTestCases()
    {
        return CompiledTestCaseResults.Where(x => x.PerformanceRunResult is not null);
    }

    public IEnumerable<CompiledTestCaseResultTrackingFormat> GetFailedTestCases()
    {
        return CompiledTestCaseResults.Where(x => x.PerformanceRunResult is null);
    }

    public ClassExecutionSummaryTrackingFormat FilterForSuccessfulTestCases()
    {
        return new ClassExecutionSummaryTrackingFormat(TestClass, ExecutionSettings, GetSuccessfulTestCases());
    }
}