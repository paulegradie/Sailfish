using System.Text.Json;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Serialization.V1;

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
            new TypePropertyConvert()
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
}