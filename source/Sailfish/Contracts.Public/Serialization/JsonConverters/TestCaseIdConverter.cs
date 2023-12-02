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

        var testCaseName = testCaseId.GetProperty("TestCaseName").Deserialize<TestCaseName>()
                           ?? throw new SailfishException("Failed to deserialize 'TestCaseName'");
        var testCaseVariables = testCaseId.GetProperty("TestCaseVariables").Deserialize<TestCaseVariables>() ??
                                throw new SailfishException("Failed to deserialize 'TestCaseVariables'");
        return new TestCaseId(testCaseName, testCaseVariables);
    }

    public override void Write(Utf8JsonWriter writer, TestCaseId? value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}