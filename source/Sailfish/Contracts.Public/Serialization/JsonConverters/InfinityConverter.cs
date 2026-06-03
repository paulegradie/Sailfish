using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Public.Serialization.JsonConverters;

public class InfinityConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Guard on the token type first: Utf8JsonReader.TryGetDouble THROWS on a string token rather than
        // returning false, and Write emits ±Infinity as strings — so reading those values back used to blow
        // up before the string handling below.
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble(out var value)) return value;

        var stringValue = reader.GetString()
                          ?? throw new JsonException("Unable to parse a null token as a double (using custom parser).");

        return stringValue switch
        {
            "Inf" => double.PositiveInfinity,
            "-Inf" => double.NegativeInfinity,
            _ when double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => throw new JsonException($"Unable to parse value (using custom parser): {stringValue}")
        };
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        // Explicit, culture-invariant tokens so Read can always parse them back.
        if (double.IsPositiveInfinity(value))
            writer.WriteStringValue("Infinity");
        else if (double.IsNegativeInfinity(value))
            writer.WriteStringValue("-Infinity");
        else
            writer.WriteNumberValue(value);
    }
}
