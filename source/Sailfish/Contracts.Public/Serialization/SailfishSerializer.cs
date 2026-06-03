using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Sailfish.Analysis.ScaleFish;
using Sailfish.Contracts.Public.Serialization.JsonConverters;

namespace Sailfish.Contracts.Public.Serialization;

public static class SailfishSerializer
{
    private static readonly List<JsonConverter> Converters = new()
    {
        new JsonNanConverter(),
        new ComplexityFunctionConverter(),
        new ExecutionSummaryTrackingFormatConverter(),
        new TypePropertyConverter()
    };

    /// <summary>
    ///     The metadata resolver Sailfish uses for every persisted contract.
    ///     <para>
    ///         In hosts where reflection-based serialization is available (the common case: local dev, CI,
    ///         Visual Studio, VS Test Explorer) the source-generated <see cref="SailfishJsonContext" /> is
    ///         combined with the reflection resolver so that <em>any</em> type still serializes exactly as it
    ///         did before — zero behaviour change.
    ///     </para>
    ///     <para>
    ///         In hosts where reflection serialization is disabled (Native AOT, <c>PublishTrimmed</c>, and
    ///         .NET 10 file-based <c>dotnet run app.cs</c> launchers) only the source-gen context is used. It
    ///         covers the full tracking-file + reproducibility-manifest persistence graph, so the post-run
    ///         pipeline no longer crashes (exit 134) the moment a tracking file is written or read back.
    ///     </para>
    /// </summary>
    internal static readonly IJsonTypeInfoResolver TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
        ? JsonTypeInfoResolver.Combine(SailfishJsonContext.Default, new DefaultJsonTypeInfoResolver())
        : SailfishJsonContext.Default;

    public static string Serialize<T>(T data, IEnumerable<JsonConverter>? converters = null)
    {
        return JsonSerializer.Serialize(data, GetOptions(converters ?? Array.Empty<JsonConverter>()));
    }

    public static T? Deserialize<T>(string serializedData, IEnumerable<JsonConverter>? converters = null)
    {
        return JsonSerializer.Deserialize<T>(serializedData, GetOptions(converters ?? Array.Empty<JsonConverter>()));
    }

    public static IList<JsonConverter> GetDefaultConverters()
    {
        return Converters;
    }

    private static JsonSerializerOptions GetOptions(IEnumerable<JsonConverter> converters)
    {
        var allConverters = new List<JsonConverter>();
        allConverters.AddRange(converters);
        allConverters.AddRange(GetDefaultConverters());
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = TypeInfoResolver
        };
        foreach (var converter in allConverters) options.Converters.Add(converter);

        return options;
    }
}