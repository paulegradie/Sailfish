using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sailfish.Contracts.Serialization.V1.Converters;

namespace Sailfish.Contracts.Serialization.V1;

/// <summary>
/// Data structure contract used specifically for serializing and deserializing tracking file data
/// Changes to this constitute a **BREAKING CHANGE** in the Sailfish data persistence contract
/// Do not make changes to this lightly
/// </summary>
public class ExecutionSummaryTrackingFormatV1
{
    [JsonConstructor]
#pragma warning disable CS8618
    public ExecutionSummaryTrackingFormatV1()
#pragma warning restore CS8618
    {
    }

    public ExecutionSummaryTrackingFormatV1(Type type, ExecutionSettingsTrackingFormat settings, IEnumerable<CompiledTestCaseResultTrackingFormatV1> compiledTestCaseResults)
    {
        Type = type;
        Settings = settings;
        CompiledTestCaseResults = compiledTestCaseResults;
    }

    [JsonConverter(typeof(TypePropertyConverter))]
    public Type Type { get; set; }
    public ExecutionSettingsTrackingFormat Settings { get; }
    public IEnumerable<CompiledTestCaseResultTrackingFormatV1> CompiledTestCaseResults { get; set; }
}