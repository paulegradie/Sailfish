using Sailfish.Exceptions;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Public.Serialization.JsonConverters;

public class TypePropertyConverter : JsonConverter<Type>
{
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var typeElement = doc.RootElement;
        var typeName = typeElement.GetString() ?? throw new SailfishException("Failed to find property: 'Type'");
        var type = Type.GetType(typeName);
        return type;
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.AssemblyQualifiedName, options);
    }
}