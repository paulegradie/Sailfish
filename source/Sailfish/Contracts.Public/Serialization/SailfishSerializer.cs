using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Serialization.V1.JsonConverters;

namespace Sailfish.Contracts.Public;

public static class SailfishSerializer
{
    private static readonly List<JsonConverter> Converters = new()
    {
        new JsonNanConverter(),
        new ComplexityFunctionConverter(),
        new ExecutionSummaryTrackingFormatConverter(),
        new TypePropertyConverter()
    };

    public static string Serialize<T>(T data, IEnumerable<JsonConverter>? converters = null)
        => JsonSerializer.Serialize(data, GetOptions(converters ?? new JsonConverter[] { }));

    public static T? Deserialize<T>(string serializedData, IEnumerable<JsonConverter>? converters = null)
        => JsonSerializer.Deserialize<T>(serializedData, GetOptions(converters ?? new JsonConverter[] { }));

    public static IList<JsonConverter> GetDefaultConverters()
        => Converters;

    private static JsonSerializerOptions GetOptions(IEnumerable<JsonConverter> converters)
    {
        var allConverters = new List<JsonConverter>();
        allConverters.AddRange(converters);
        allConverters.AddRange(GetDefaultConverters());
        var options = new JsonSerializerOptions()
        {
            WriteIndented = true,
        };
        foreach (var converter in allConverters)
        {
            options.Converters.Add(converter);
        }

        return options;
    }
}