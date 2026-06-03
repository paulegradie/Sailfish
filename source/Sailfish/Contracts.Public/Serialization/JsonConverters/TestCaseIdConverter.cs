using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sailfish.Contracts.Public.Models;
using Sailfish.Exceptions;

namespace Sailfish.Contracts.Public.Serialization.JsonConverters;

public class TestCaseIdConverter : JsonConverter<TestCaseId?>
{
    public override TestCaseId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var testCaseId = doc.RootElement;

        // Pass options through so nested deserialization resolves via the same (source-gen) resolver
        // rather than the reflection-based default, which is disabled in AOT / trimmed / file-based hosts.
        var testCaseName = testCaseId.GetProperty("TestCaseName").Deserialize<TestCaseName>(options)
                           ?? throw new SailfishException("Failed to deserialize 'TestCaseName'");
        var testCaseVariables = testCaseId.GetProperty("TestCaseVariables").Deserialize<TestCaseVariables>(options) ??
                                throw new SailfishException("Failed to deserialize 'TestCaseVariables'");
        return new TestCaseId(testCaseName, testCaseVariables);
    }

    public override void Write(Utf8JsonWriter writer, TestCaseId? value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}