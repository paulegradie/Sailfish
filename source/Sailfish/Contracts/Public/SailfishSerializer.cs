using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Serialization.V1;
using Sailfish.Contracts.Serialization.V1.Converters;

namespace Sailfish.Contracts.Public;

public static class SailfishSerializer
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        WriteIndented = true, // Add this line to enable indented JSON
        Converters =
        {
            new JsonNanConverter(),
            new ComplexityFunctionConverter(),
            new ExecutionSummaryTrackingFormatV1Converter(),
            new TypePropertyConverter()
        }
    };

    public static string Serialize<T>(T data)
    {
        return JsonSerializer.Serialize(data, Options);
    }

    public static T? Deserialize<T>(string serializedData)
    {
        return JsonSerializer.Deserialize<T>(serializedData, Options);
    }

    public static IList<JsonConverter> GetConverters()
    {
        return Options.Converters;
    }
}