using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Public.Serialization.JsonConverters;

public class JsonNanConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Numbers come straight through. The token-type guard is essential: Utf8JsonReader.TryGetDouble
        // THROWS InvalidOperationException ("Cannot get the value of a token type 'String' as a number")
        // when the current token is a string — it does not return false. Since Write emits NaN / ±Infinity
        // as JSON strings, calling TryGetDouble on those used to blow up before the string handling below was
        // ever reached, which made any tracking file containing a NaN/Infinity double unreadable.
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetDouble(out var value)) return value;

        var stringValue = reader.GetString()
                          ?? throw new JsonException("Unable to parse a null token as a double (using custom parser).");

        return stringValue switch
        {
            // Historical short forms — kept for backward compatibility with older tracking files.
            "Inf" => double.PositiveInfinity,
            "-Inf" => double.NegativeInfinity,
            // "NaN", "Infinity" and "-Infinity" (the forms Write emits) are recognized natively by
            // double.TryParse against the invariant culture.
            _ when double.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => throw new JsonException($"Unable to parse value (using custom parser): {stringValue}")
        };
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        // NaN / ±Infinity are not valid JSON numbers, so emit them as strings. Use explicit, culture-invariant
        // tokens (rather than value.ToString(), which is culture-dependent) so Read can always parse them back.
        if (double.IsNaN(value))
            writer.WriteStringValue("NaN");
        else if (double.IsPositiveInfinity(value))
            writer.WriteStringValue("Infinity");
        else if (double.IsNegativeInfinity(value))
            writer.WriteStringValue("-Infinity");
        else
            writer.WriteNumberValue(value);
    }
}
