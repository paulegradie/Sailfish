using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sailfish.Contracts.Serialization.V1.Converters;

public class InfinityConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TryGetDouble(out var value))
        {
            return value;
        }

        return reader.GetString() == double.PositiveInfinity.ToString()
            ? double.PositiveInfinity
            : double.NegativeInfinity;
    }

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        if (double.IsInfinity(value))
        {
            writer.WriteStringValue(value.ToString());
        }
        else
        {
            writer.WriteNumberValue(value);
        }
    }
}