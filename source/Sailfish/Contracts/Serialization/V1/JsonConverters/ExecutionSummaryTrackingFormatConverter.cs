using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sailfish.Exceptions;

namespace Sailfish.Contracts.Serialization.V1.JsonConverters;

public class ExecutionSummaryTrackingFormatV1Converter : JsonConverter<ClassExecutionSummaryTrackingFormat>
{
    public override ClassExecutionSummaryTrackingFormat? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var jsonElementClassExecutionSummary = doc.RootElement;

        var typeName = jsonElementClassExecutionSummary.GetProperty(nameof(TrackingFileSerializationHelper.TestClass)).GetString() ??
                       throw new SailfishException("Failed to find property: 'Type'");

        var type = Type.GetType(typeName);

        var settings = jsonElementClassExecutionSummary
            .GetProperty(nameof(TrackingFileSerializationHelper.ExecutionSettings))
            .Deserialize<ExecutionSettingsTrackingFormat>() ?? throw new SailfishException("Failed to deserialize 'Settings'");
        var compiledTestCaseResults = jsonElementClassExecutionSummary
            .GetProperty(nameof(TrackingFileSerializationHelper.CompiledTestCaseResults))
            .Deserialize<IEnumerable<CompiledTestCaseResultTrackingFormat>>() ?? throw new SailfishException("Failed to deserialize 'CompiledTestCaseResults'");

        var testClassStart = jsonElementClassExecutionSummary
            .GetProperty(nameof(TrackingFileSerializationHelper.))

        return new ClassExecutionSummaryTrackingFormat(type!, settings, compiledTestCaseResults);
    }

    public override void Write(Utf8JsonWriter writer, ClassExecutionSummaryTrackingFormat value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(
            writer,
            new TrackingFileSerializationHelper(
                value.TestClass.AssemblyQualifiedName ?? string.Empty,
                value.ExecutionSettings,
                value.CompiledTestCaseResults),
            options);
    }


    /// <summary>
    /// A transfer class that lets us move between the Type type for the Type property and string type for the Type property
    /// NOTE: Names on this class MUST match the ExecutionSummaryTrackingFormat property names
    /// </summary>
    internal class TrackingFileSerializationHelper
    {
        [JsonConstructor]
#pragma warning disable CS8618
        public TrackingFileSerializationHelper()
#pragma warning restore CS8618
        {
        }

        public TrackingFileSerializationHelper(
            string testClass,
            ExecutionSettingsTrackingFormat executionSettings,
            IEnumerable<CompiledTestCaseResultTrackingFormat> compiledTestCaseResults,
            DateTimeOffset testClassStart,
            DateTimeOffset testClassStop)
        {
            TestClass = testClass;
            ExecutionSettings = executionSettings;
            CompiledTestCaseResults = compiledTestCaseResults;
            TestClassStart = testClassStart;
            TestClassStop = testClassStop;
            TestClassDuration = TestClassStop - TestClassStart;
        }

        public string TestClass { get; set; }
        public ExecutionSettingsTrackingFormat ExecutionSettings { get; }
        public IEnumerable<CompiledTestCaseResultTrackingFormat> CompiledTestCaseResults { get; set; }
        public DateTimeOffset TestClassStart { get; set; }
        public DateTimeOffset TestClassStop { get; set; }
        public TimeSpan TestClassDuration { get; set; }
    }
}