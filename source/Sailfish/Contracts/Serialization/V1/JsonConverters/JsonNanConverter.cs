using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Serialization.V1.JsonConverters;

public class JsonNanConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TryGetDouble(out var value))
        {
            return value;
        }

        var stringValue = reader.GetString();
        if (stringValue != null && stringValue.Equals("NaN", StringComparison.InvariantCultureIgnoreCase))
        {
            return double.NaN;
        }

        return stringValue switch
        {
            "NaN" => double.NaN,
            "Inf" => double.PositiveInfinity,
            "-Inf" => double.NegativeInfinity,
            _ => throw new JsonException($"Unable to parse value (using custom parser): {stringValue}")
        };
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (double.IsNaN(value))
        {
            writer.WriteStringValue("NaN");
        }
        else if (double.IsInfinity(value))
        {
            writer.WriteStringValue(value.ToString());
        }
        else
        {
            writer.WriteNumberValue(value);
        }
    }
}