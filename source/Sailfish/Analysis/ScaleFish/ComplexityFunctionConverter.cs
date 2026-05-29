using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Sailfish.Analysis.ScaleFish.CurveFitting;
using Sailfish.Contracts.Public.Serialization.JsonConverters;
using Sailfish.Exceptions;

namespace Sailfish.Analysis.ScaleFish;

public class ComplexityFunctionConverter : JsonConverter<List<ScalefishClassModel>>
{
    public override List<ScalefishClassModel> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var results = new List<ScalefishClassModel>();
        foreach (var testClassElement in root.EnumerateArray())
        {
            var testNameSpace = testClassElement.GetProperty(nameof(ScalefishClassModel.NameSpace)).GetString() ??
                                throw new SailfishException($"Failed to find '{nameof(ScalefishClassModel.NameSpace)}'");
            var testClassName = testClassElement.GetProperty(nameof(ScalefishClassModel.TestClassName)).GetString() ??
                                throw new SailfishException($"Failed to find property: '{nameof(ScalefishClassModel.TestClassName)}'");
            var testMethodComplexityResults = new List<ScaleFishMethodModel>();

            foreach (var testMethodElement in testClassElement.GetProperty(nameof(ScalefishClassModel.ScaleFishMethodModels)).EnumerateArray())
            {
                var testMethodName = testMethodElement.GetProperty(nameof(ScaleFishMethodModel.TestMethodName)).GetString() ??
                                     throw new SailfishException($"Failed to find property '{nameof(ScaleFishMethodModel.TestMethodName)}'");

                var testPropertyComplexityResults = new List<ScaleFishPropertyModel>();

                foreach (var testPropertyElement in testMethodElement.GetProperty(nameof(ScaleFishMethodModel.ScaleFishPropertyModels)).EnumerateArray())
                {
                    var propertyName = testPropertyElement.GetProperty(nameof(ScaleFishPropertyModel.PropertyName)).GetString() ??
                                       throw new SailfishException($"Failed to find property '{nameof(ScaleFishPropertyModel.PropertyName)}'");
                    var complexityResultJsonElement = testPropertyElement.GetProperty(nameof(ScaleFishPropertyModel.ScaleFishModel));

                    var complexityFunctionProperty = complexityResultJsonElement.GetProperty(nameof(ScaleFishModel.ScaleFishModelFunction));
                    var complexityFunctionTypeName = complexityFunctionProperty.GetProperty(nameof(ScaleFishModelFunction.Name)).GetString();
                    if (complexityFunctionTypeName is null) throw new SerializationException($"Failed to find {nameof(ScaleFishModelFunction.Name)} property");
                    var complexityFunction = DeserializeComplexityFunction(complexityFunctionTypeName, complexityFunctionProperty);
                    var goodnessOfFit = complexityResultJsonElement.GetProperty(nameof(ScaleFishModel.GoodnessOfFit)).GetDouble();

                    var nextBestComplexityFunctionProperty = complexityResultJsonElement.GetProperty(nameof(ScaleFishModel.NextClosestScaleFishModelFunction));
                    var nextBestComplexityFunctionTypeName = nextBestComplexityFunctionProperty.GetProperty(nameof(ScaleFishModelFunction.Name)).GetString();
                    if (nextBestComplexityFunctionTypeName is null) throw new SerializationException($"Failed to find {nameof(ScaleFishModelFunction.Name)} property");
                    var nextBestComplexityFunction = DeserializeComplexityFunction(nextBestComplexityFunctionTypeName, nextBestComplexityFunctionProperty);
                    var nextBestGoodnessOfFit = complexityResultJsonElement.GetProperty(nameof(ScaleFishModel.NextClosestGoodnessOfFit)).GetDouble();

                    // Phase-2 fields — all optional in serialized form so older model files keep loading.
                    var bestAicc = ReadOptionalDouble(complexityResultJsonElement, nameof(ScaleFishModel.BestAicc), double.NaN);
                    var nextBestAicc = ReadOptionalDouble(complexityResultJsonElement, nameof(ScaleFishModel.NextBestAicc), double.NaN);
                    var akaikeWeight = ReadOptionalDouble(complexityResultJsonElement, nameof(ScaleFishModel.AkaikeWeight), double.NaN);
                    var isDistinguishable = ReadOptionalBool(complexityResultJsonElement, nameof(ScaleFishModel.IsDistinguishable), false);
                    var sampleSize = ReadOptionalInt(complexityResultJsonElement, nameof(ScaleFishModel.SampleSize), 0);
                    var powerLog = ReadOptionalPowerLog(complexityResultJsonElement);
                    var bootstrap = ReadOptionalBootstrap(complexityResultJsonElement);
                    var suggestedNextN = ReadOptionalNullableInt(complexityResultJsonElement, nameof(ScaleFishModel.SuggestedNextN));
                    var crossValidation = ReadOptionalCrossValidation(complexityResultJsonElement);
                    var tailFits = ReadOptionalTailFits(complexityResultJsonElement);

                    var complexityResult = new ScaleFishModel(
                        complexityFunction,
                        goodnessOfFit,
                        nextBestComplexityFunction,
                        nextBestGoodnessOfFit,
                        bestAicc: bestAicc,
                        nextBestAicc: nextBestAicc,
                        akaikeWeight: akaikeWeight,
                        isDistinguishable: isDistinguishable,
                        sampleSize: sampleSize,
                        powerLog: powerLog)
                    {
                        Bootstrap = bootstrap,
                        SuggestedNextN = suggestedNextN,
                        CrossValidation = crossValidation,
                        TailFits = tailFits
                    };

                    testPropertyComplexityResults.Add(new ScaleFishPropertyModel(propertyName, complexityResult));
                }

                testMethodComplexityResults.Add(new ScaleFishMethodModel(testMethodName, testPropertyComplexityResults));
            }

            results.Add(new ScalefishClassModel(testNameSpace, testClassName, testMethodComplexityResults));
        }

        return results;
    }

    public override void Write(Utf8JsonWriter writer, List<ScalefishClassModel> value, JsonSerializerOptions options)
    {
        var opts = new JsonSerializerOptions();
        var converters = new List<JsonConverter>
        {
            new JsonNanConverter(),
            new ExecutionSummaryTrackingFormatConverter(),
            new TypePropertyConverter()
        };
        foreach (var converter in converters) opts.Converters.Add(converter);

        JsonSerializer.Serialize(writer, value, opts);
    }

    private static ScaleFishModelFunction DeserializeComplexityFunction(string complexityFunctionTypeName, JsonElement complexityFunctionProperty)
    {
        var deserialized = ComplexityFunctionRegistry.Deserialize(complexityFunctionTypeName, complexityFunctionProperty);
        if (deserialized is null)
        {
            // Registry lookup failed — either the name was unknown or its registered deserializer
            // returned null. Surface as a SerializationException so the loader can show the user what
            // went wrong and which name was offending.
            throw new SerializationException($"Failed to deserialize complexity function: {complexityFunctionTypeName}. Is it registered in ComplexityFunctionRegistry?");
        }
        return deserialized;
    }

    private static double ReadOptionalDouble(JsonElement element, string propertyName, double fallback)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return fallback;
        if (prop.ValueKind == JsonValueKind.Null) return fallback;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var num)) return num;
        // JsonNanConverter writes NaN / ±Infinity as strings; handle those here so we round-trip.
        if (prop.ValueKind == JsonValueKind.String)
        {
            var s = prop.GetString();
            return s switch
            {
                "NaN" => double.NaN,
                "Infinity" => double.PositiveInfinity,
                "-Infinity" => double.NegativeInfinity,
                _ => double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback
            };
        }
        return fallback;
    }

    private static bool ReadOptionalBool(JsonElement element, string propertyName, bool fallback)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return fallback;
        return prop.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => fallback
        };
    }

    private static int ReadOptionalInt(JsonElement element, string propertyName, int fallback)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return fallback;
        if (prop.ValueKind == JsonValueKind.Null) return fallback;
        return prop.TryGetInt32(out var v) ? v : fallback;
    }

    private static int? ReadOptionalNullableInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.Null) return null;
        return prop.TryGetInt32(out var v) ? v : (int?)null;
    }

    private static PowerLogResult? ReadOptionalPowerLog(JsonElement parent)
    {
        if (!parent.TryGetProperty(nameof(ScaleFishModel.PowerLog), out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.Null || prop.ValueKind != JsonValueKind.Object) return null;

        var a = ReadOptionalDouble(prop, nameof(PowerLogResult.A), double.NaN);
        var b = ReadOptionalDouble(prop, nameof(PowerLogResult.B), double.NaN);
        var c = ReadOptionalDouble(prop, nameof(PowerLogResult.C), double.NaN);
        var d = ReadOptionalDouble(prop, nameof(PowerLogResult.D), double.NaN);
        var r2 = ReadOptionalDouble(prop, nameof(PowerLogResult.RSquared), double.NaN);
        return new PowerLogResult(a, b, c, d, r2);
    }

    private static IReadOnlyList<TailFitResult> ReadOptionalTailFits(JsonElement parent)
    {
        if (!parent.TryGetProperty(nameof(ScaleFishModel.TailFits), out var prop)) return Array.Empty<TailFitResult>();
        if (prop.ValueKind != JsonValueKind.Array) return Array.Empty<TailFitResult>();

        var list = new List<TailFitResult>();
        foreach (var item in prop.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            list.Add(new TailFitResult(
                percentile: ReadOptionalDouble(item, nameof(TailFitResult.Percentile), double.NaN),
                bestFamilyName: item.TryGetProperty(nameof(TailFitResult.BestFamilyName), out var bf) ? bf.GetString() ?? "" : "",
                bestFamilyOName: item.TryGetProperty(nameof(TailFitResult.BestFamilyOName), out var bfo) ? bfo.GetString() ?? "" : "",
                bestRSquared: ReadOptionalDouble(item, nameof(TailFitResult.BestRSquared), double.NaN),
                nextFamilyName: item.TryGetProperty(nameof(TailFitResult.NextFamilyName), out var nf) ? nf.GetString() ?? "" : "",
                nextRSquared: ReadOptionalDouble(item, nameof(TailFitResult.NextRSquared), double.NaN),
                bestAicc: ReadOptionalDouble(item, nameof(TailFitResult.BestAicc), double.NaN),
                nextBestAicc: ReadOptionalDouble(item, nameof(TailFitResult.NextBestAicc), double.NaN),
                akaikeWeight: ReadOptionalDouble(item, nameof(TailFitResult.AkaikeWeight), double.NaN),
                isDistinguishable: ReadOptionalBool(item, nameof(TailFitResult.IsDistinguishable), false),
                sampleSize: ReadOptionalInt(item, nameof(TailFitResult.SampleSize), 0),
                bestScale: ReadOptionalDouble(item, nameof(TailFitResult.BestScale), double.NaN),
                bestBias: ReadOptionalDouble(item, nameof(TailFitResult.BestBias), double.NaN)));
        }
        return list;
    }

    private static CrossValidationDiagnostic? ReadOptionalCrossValidation(JsonElement parent)
    {
        if (!parent.TryGetProperty(nameof(ScaleFishModel.CrossValidation), out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.Null || prop.ValueKind != JsonValueKind.Object) return null;

        var foldCount = ReadOptionalInt(prop, nameof(CrossValidationDiagnostic.FoldCount), 0);
        var rankAgreement = ReadOptionalDouble(prop, nameof(CrossValidationDiagnostic.RankAgreement), double.NaN);
        var meanError = ReadOptionalDouble(prop, nameof(CrossValidationDiagnostic.MeanPredictionError), double.NaN);
        var medianError = ReadOptionalDouble(prop, nameof(CrossValidationDiagnostic.MedianPredictionError), double.NaN);
        return new CrossValidationDiagnostic(foldCount, rankAgreement, meanError, medianError);
    }

    private static BootstrapDiagnostic? ReadOptionalBootstrap(JsonElement parent)
    {
        if (!parent.TryGetProperty(nameof(ScaleFishModel.Bootstrap), out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.Null || prop.ValueKind != JsonValueKind.Object) return null;

        var iterations = ReadOptionalInt(prop, nameof(BootstrapDiagnostic.Iterations), 0);
        var selectionAgreement = ReadOptionalDouble(prop, nameof(BootstrapDiagnostic.SelectionAgreement), double.NaN);
        var scaleLow = ReadOptionalDouble(prop, nameof(BootstrapDiagnostic.ScaleCiLower), double.NaN);
        var scaleHigh = ReadOptionalDouble(prop, nameof(BootstrapDiagnostic.ScaleCiUpper), double.NaN);
        var biasLow = ReadOptionalDouble(prop, nameof(BootstrapDiagnostic.BiasCiLower), double.NaN);
        var biasHigh = ReadOptionalDouble(prop, nameof(BootstrapDiagnostic.BiasCiUpper), double.NaN);
        return new BootstrapDiagnostic(iterations, selectionAgreement, scaleLow, scaleHigh, biasLow, biasHigh);
    }
}