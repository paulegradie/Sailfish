using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sailfish.Exceptions;

namespace Sailfish.Contracts.Serialization.V1.Converters;

public class ExecutionSummaryTrackingFormatV1Converter : JsonConverter<ExecutionSummaryTrackingFormatV1>
{
    public override ExecutionSummaryTrackingFormatV1? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var jsonElementForExecutionSummaryTrackingFormatV1 = doc.RootElement;

        var typeName = jsonElementForExecutionSummaryTrackingFormatV1.GetProperty("Type").GetString() ?? throw new SailfishException("Failed to find property: 'Type'");
        var type = Type.GetType(typeName);

        var settings = jsonElementForExecutionSummaryTrackingFormatV1
            .GetProperty("Settings")
            .Deserialize<ExecutionSettingsTrackingFormat>() ?? throw new SailfishException("Failed to deserialize 'Settings'");
        var compiledTestCaseResults = jsonElementForExecutionSummaryTrackingFormatV1
            .GetProperty("CompiledTestCaseResults")
            .Deserialize<IEnumerable<CompiledTestCaseResultTrackingFormatV1>>() ?? throw new SailfishException("Failed to deserialize 'CompiledTestCaseResults'");

        return new ExecutionSummaryTrackingFormatV1(type!, settings, compiledTestCaseResults);
    }

    public override void Write(Utf8JsonWriter writer, ExecutionSummaryTrackingFormatV1 value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, new TrackingFileSerializationHelper(value.Type.AssemblyQualifiedName ?? string.Empty, value.Settings, value.CompiledTestCaseResults),
            options);
    }
}